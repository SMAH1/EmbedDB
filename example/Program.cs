using System.Text.Json.Serialization;

namespace EmbedDB.Usage;

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
    public int Stock { get; set; }
}

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(User))]
[JsonSerializable(typeof(Product))]
internal partial class AppJsonContext : JsonSerializerContext { }

public class Program
{
    public static void Main()
    {
        string dbFile = "db";

        using var db = new EmbedDB.EmbedDatabase(dbFile);

        db
          .Register(AppJsonContext.Default.User)
          .Register(AppJsonContext.Default.Product);

        db.Initialize();

        var users = db.GetCollection<User>();
        var products = db.GetCollection<Product>();

        // ==========================================
        // 1. Initial Data Insertion
        // ==========================================
        Console.WriteLine("--- Initial Data Insertion ---");

        if (users.Snapshot.Count() == 0)
        {
            users.Add(new User { Id = 1, Name = "Ali", Age = 25 });
            users.AddRange(new[] {
            new User { Id = 2, Name = "Reza", Age = 30 },
            new User { Id = 3, Name = "Sara", Age = 22 },
            new User { Id = 4, Name = "Maryam", Age = 17 } // Underage
        });

            products.AddRange(new[] {
            new Product { Id = 101, Title = "Laptop", Price = 1500.50m, Stock = 10 },
            new Product { Id = 102, Title = "Mouse", Price = 25.00m, Stock = 50 },
            new Product { Id = 103, Title = "Keyboard", Price = 45.00m, Stock = 30 },
            new Product { Id = 104, Title = "Monitor", Price = 300.00m, Stock = 0 } // Out of stock
        });

            PrintState(users, products);
        }

        // ==========================================
        // 2. Update Operations
        // ==========================================
        Console.WriteLine("\n--- Update Operations ---");

        // Update single entity (Change Ali's name and age)
        users.Update(
            match: u => u.Id == 1,
            mutator: u =>
            {
                u.Name = "Ali Mohammadi";
                u.Age = 26;
            }
        );
        Console.WriteLine("Updated User ID 1.");

        // Update multiple entities (Apply 10% discount to all products under $50)
        products.Update(
            match: p => p.Price < 50,
            mutator: p => p.Price = Math.Round(p.Price * 0.9m, 2)
        );
        Console.WriteLine("Applied 10% discount to cheap products.");

        PrintState(users, products);

        // ==========================================
        // 3. Delete Operations
        // ==========================================
        Console.WriteLine("\n--- Delete Operations ---");

        // Delete users under 18
        int deletedUsersCount = users.Delete(u => u.Age < 18);
        Console.WriteLine($"Deleted {deletedUsersCount} underage user(s).");

        // Delete out-of-stock products
        int deletedProductsCount = products.Delete(p => p.Stock == 0);
        Console.WriteLine($"Deleted {deletedProductsCount} out-of-stock product(s).");

        PrintState(users, products);

        // ==========================================
        // 4. Complex Queries
        // ==========================================
        Console.WriteLine("\n--- Complex Queries ---");

        var averageAge = users.Query(list => list.Average(u => u.Age));
        Console.WriteLine($"Average user age: {averageAge:F1}");

        var totalInventoryValue = products.Query(list => list.Sum(p => p.Price * p.Stock));
        Console.WriteLine($"Total inventory value: ${totalInventoryValue:F2}");

        var expensiveItems = products.Query(list =>
            list.Where(p => p.Price > 100).Select(p => p.Title).ToList()
        );
        Console.WriteLine($"Expensive items: {string.Join(", ", expensiveItems)}");

        Console.WriteLine("\nDatabase operations completed successfully.");
    }

    private static void PrintState(IEmbedDBCollection<User> users, IEmbedDBCollection<Product> products)
    {
        Console.WriteLine("\n[Current Database State]");

        Console.WriteLine("Users:");
        if (!users.Snapshot.Any()) Console.WriteLine("  (Empty)");
        foreach (var u in users.Snapshot)
        {
            Console.WriteLine($"  - ID:{u.Id} | {u.Name} | Age: {u.Age}");
        }

        Console.WriteLine("Products:");
        if (!products.Snapshot.Any()) Console.WriteLine("  (Empty)");
        foreach (var p in products.Snapshot)
        {
            Console.WriteLine($"  - ID:{p.Id} | {p.Title,-10} | ${p.Price,-8} | Stock: {p.Stock}");
        }
        Console.WriteLine(new string('-', 40));
    }
}