namespace EmbedDB;

public interface IEmbedDBCollection<T>
{
    IReadOnlyList<T> Snapshot { get; }
    TResult Query<TResult>(Func<IReadOnlyList<T>, TResult> query);

    void Add(T item);
    void AddRange(IEnumerable<T> items);
    void Update(Predicate<T> match, Action<T> mutator);
    int Delete(Predicate<T> match);
}
