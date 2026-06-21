
# EmbedDB (Embedded AOT Document Database)

**EmbedDB** is a lightweight, high-performance, in-memory embedded document database designed specifically for **.NET 9.0** with **100% Native AOT (Ahead-of-Time)** compilation support. 

It keeps all data in memory for blazing-fast access while providing durable, crash-safe persistence to disk using compressed (GZipped) JSON files.

## 🌟 Key Features

- **100% Native AOT Compatible:** Zero reflection used at runtime. Relies entirely on `System.Text.Json` Source Generators.
- **In-Memory Document Store:** Stores collections of POCOs (Plain Old CLR Objects) in memory for microsecond query times.
- **MVCC (Multi-Version Concurrency Control):** Implements Copy-On-Write (COW) snapshot isolation. Readers never block writers, and writers never block readers.
- **Thread-Safe:** Utilizes `ReaderWriterLockSlim` to allow multiple concurrent readers and exclusive writers.
- **Crash-Safe Atomic Saves:** Uses a "write-to-temp and atomic-rename" strategy combined with `FileStream.Flush(true)` to guarantee zero data corruption during power failures or system crashes.
- **Compressed Persistence:** Data is persisted as GZipped JSON to minimize disk footprint.
- **Strict Mutation Control:** Exposes read-only snapshots (`IReadOnlyList<T>`) for LINQ queries. All modifications (Add/Update/Delete) are strictly controlled through the database engine to maintain state integrity.

## 📋 Prerequisites

- **.NET 9.0 SDK** or higher

## 🚀 Quick Start

### 1. Define Your Models and JSON Context
Because AOT does not support runtime reflection, you must define your entities and register them in a `JsonSerializerContext`.

```csharp
using System.Text.Json.Serialization;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Price { get; set; }
}

// Register ONLY the entity types (not List<T>)
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(Product))]
internal partial class AppJsonContext : JsonSerializerContext { }
```

### 2. Initialize the Database
Register your entities using the fluent API and initialize the database to load existing data from the disk.

```csharp
using var db = new EmbedDatabase("my_data.db.gz"); // or empty string for in-memory only

db.Register(AppJsonContext.Default.User)
  .Register(AppJsonContext.Default.Product);

db.Initialize(); // Loads data from disk into memory
```

### 3. Perform CRUD Operations
Get strongly-typed collections and perform thread-safe mutations.

```csharp
var users = db.GetCollection<User>();
var products = db.GetCollection<Product>();

// Add
users.Add(new User { Id = 1, Name = "Alice", Age = 28 });

// Add Range
products.AddRange(new[] {
    new Product { Id = 101, Title = "Laptop", Price = 1200.00m },
    new Product { Id = 102, Title = "Mouse", Price = 25.50m }
});

// Update (Conditional)
users.Update(
    match: u => u.Id == 1, 
    mutator: u => u.Age = 29
);

// Delete (Conditional)
int deletedCount = products.Delete(p => p.Price < 10.0m);
```

### 4. Querying Data (LINQ)
Queries are executed against an immutable, read-only snapshot of the current state.

```csharp
// Simple count
int totalUsers = users.Query.Count();

// Complex LINQ
var expensiveItems = products.Query
    .Where(p => p.Price > 100)
    .OrderByDescending(p => p.Price)
    .Select(p => p.Title)
    .ToList()
);
```

## 🏗️ Architecture & Design Decisions

### Concurrency & MVCC
The database uses a **Copy-On-Write (COW)** pattern. When a write operation occurs, the engine creates a shallow copy of the affected collection, applies the mutations to the copy, and then atomically swaps the memory reference. 
* **Readers** receive a snapshot of the list reference at the exact moment they requested it. They never experience locking delays.
* **Writers** hold an exclusive lock only during the mutation and the subsequent disk I/O operation.

### Durability & Crash Safety
Prevent file corruption during unexpected shutdowns.
If a crash occurs during saving data, the main database file remains untouched and intact.

## 🧭 Roadmap (TODO)

We are continuously working to improve **EmbedDB**. Here are the planned features for upcoming releases:

- **Strict Deep Immutability Enforcement:** 
  Currently, the *collections* are strictly read-only, but the *entities* inside the snapshot remain mutable reference types. Future versions will enforce deep immutability. This will be achieved by either requiring immutable C# `record` types, returning deep-cloned objects/proxies on read, or implementing structural sharing. This guarantees that data can **only** be modified through the explicit `Update` pipeline, completely eliminating accidental side-effects or unauthorized mutations from external code.

- **WAL (Write-Ahead Logging):**
  Transitioning from full-state atomic saves to a Write-Ahead Log mechanism for even faster write operations and point-in-time recovery capabilities.

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

## 🤝 Contributing
Contributions, issues, and feature requests are welcome! Feel free to open an issue or submit a pull request.
