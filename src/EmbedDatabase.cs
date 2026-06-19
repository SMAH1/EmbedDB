using System.Collections.Concurrent;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace EmbedDB;

public class EmbedDatabase : IDisposable
{
    private readonly string _filePath;
    private readonly string _tempFilePath; // Temp file for atomic writes
    private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.NoRecursion);
    private readonly ConcurrentDictionary<string, IEmbedDBCollectionExtend> _collections = new();
    private bool _isInitialized = false;

    public EmbedDatabase(string filePath)
    {
        _filePath = filePath;
        _tempFilePath = _filePath + ".tmp";
    }

    public EmbedDatabase Register<T>(JsonTypeInfo<T> typeInfo)
    {
        if (_isInitialized)
            throw new InvalidOperationException("Cannot register entities after initialization.");

        var name = typeof(T).Name;
        var wrapper = new EmbedDBCollection<T>(this, name, typeInfo);

        if (!_collections.TryAdd(name, wrapper))
            throw new ArgumentException($"Entity '{name}' is already registered.");

        return this;
    }

    public void Initialize()
    {
        if (_isInitialized) return;
        Load();
        _isInitialized = true;
    }

    public IEmbedDBCollection<T> GetCollection<T>()
    {
        if (!_isInitialized)
            throw new InvalidOperationException("Database must be initialized before getting collections.");

        var name = typeof(T).Name;

        if (_collections.TryGetValue(name, out var wrapper))
            return (IEmbedDBCollection<T>)wrapper;

        throw new KeyNotFoundException($"Entity '{name}' is not registered.");
    }

    internal TResult ExecuteRead<TResult>(Func<TResult> func)
    {
        _lock.EnterReadLock();
        try
        {
            return func();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    internal void ExecuteWrite(Action action)
    {
        _lock.EnterWriteLock();
        try
        {
            action();
            Save();
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void Load()
    {
        // 1. Cleanup leftover temp file from a previous crash
        if (File.Exists(_tempFilePath))
        {
            try { File.Delete(_tempFilePath); } catch { /* Ignore */ }
        }

        if (!File.Exists(_filePath)) return;

        _lock.EnterWriteLock();
        try
        {
            using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var gz = new GZipStream(fs, CompressionMode.Decompress);
            using var doc = JsonDocument.Parse(gz);

            var root = doc.RootElement;

            foreach (var kvp in _collections)
            {
                if (root.TryGetProperty(kvp.Key, out var prop))
                {
                    kvp.Value.DeserializeData(prop);
                }
            }
        }
        catch (Exception ex)
        {
            // 2. If the main file is somehow corrupted, log and start fresh.
            // In production, you might want to move the corrupted file to a .bak file.
            Console.WriteLine($"[DB] Warning: Database file is corrupted ({ex.Message}). Starting with empty state.");
            try { File.Move(_filePath, _filePath + ".corrupted"); } catch { }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    private void Save()
    {
        // 1. Write to temporary file
        using (var fs = new FileStream(_tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            using var gz = new GZipStream(fs, CompressionLevel.Fastest);
            using var writer = new Utf8JsonWriter(gz);

            writer.WriteStartObject();
            writer.WriteNumber("timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            foreach (var kvp in _collections)
            {
                writer.WritePropertyName(kvp.Key);
                kvp.Value.SerializeData(writer);
            }

            writer.WriteEndObject();
            writer.Flush();

            // 2. Flush to physical disk before renaming
            fs.Flush(true);
        }
        // Streams are fully closed and disposed here

        // 3. Atomic replace (Safe Save)
        // File.Replace is atomic on modern file systems (NTFS, ext4, APFS).
        if (File.Exists(_filePath))
        {
            File.Replace(_tempFilePath, _filePath, null);
        }
        else
        {
            File.Move(_tempFilePath, _filePath);
        }
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}
