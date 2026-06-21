namespace EmbedDB;

public interface IEmbedDBCollection<T>
{
    IReadOnlyList<T> Query { get; }

    void Add(T item);
    void AddRange(IEnumerable<T> items);
    void Update(Predicate<T> match, Action<T> mutator);
    int Delete(Predicate<T> match);
}
