namespace WideCollections;

public interface IWideReadOnlyCollection<out T> : IEnumerable<T> {
    long Count { get; }
}