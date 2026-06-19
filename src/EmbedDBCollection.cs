using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace EmbedDB;

internal class EmbedDBCollection<T> : IEmbedDBCollection<T>, IEmbedDBCollectionExtend
{
    private readonly EmbedDatabase _db;
    private readonly JsonTypeInfo<T> _jsonTypeInfo;
    private List<T> _currentList = new();

    internal EmbedDBCollection(EmbedDatabase db, string name, JsonTypeInfo<T> jsonTypeInfo)
    {
        _db = db;
        Name = name;
        _jsonTypeInfo = jsonTypeInfo;
    }

    public string Name { get; }

    public IReadOnlyList<T> Snapshot => _currentList;

    public TResult Query<TResult>(Func<IReadOnlyList<T>, TResult> query)
    {
        return _db.ExecuteRead(() => query(_currentList));
    }

    public void Add(T item)
    {
        _db.ExecuteWrite(() =>
        {
            var newList = new List<T>(_currentList) { item };
            _currentList = newList;
        });
    }

    public void AddRange(IEnumerable<T> items)
    {
        _db.ExecuteWrite(() =>
        {
            var newList = new List<T>(_currentList);
            newList.AddRange(items);
            _currentList = newList;
        });
    }

    public void Update(Predicate<T> match, Action<T> mutator)
    {
        _db.ExecuteWrite(() =>
        {
            var newList = new List<T>(_currentList);
            bool changed = false;
            for (int i = 0; i < newList.Count; i++)
            {
                if (match(newList[i]))
                {
                    mutator(newList[i]);
                    changed = true;
                }
            }
            if (changed) _currentList = newList;
        });
    }

    public int Delete(Predicate<T> match)
    {
        int removedCount = 0;
        _db.ExecuteWrite(() =>
        {
            var newList = new List<T>(_currentList);
            removedCount = newList.RemoveAll(match);
            if (removedCount > 0) _currentList = newList;
        });
        return removedCount;
    }

    public void SerializeData(Utf8JsonWriter writer)
    {
        writer.WriteStartArray();
        foreach (var item in _currentList)
        {
            JsonSerializer.Serialize(writer, item, _jsonTypeInfo);
        }
        writer.WriteEndArray();
    }

    public void DeserializeData(JsonElement element)
    {
        var list = new List<T>();
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var itemElement in element.EnumerateArray())
            {
                var item = JsonSerializer.Deserialize(itemElement, _jsonTypeInfo);
                if (item != null) list.Add(item);
            }
        }
        _currentList = list;
    }
}
