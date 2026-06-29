namespace com.hafthor.WideCollections;

public interface IWideReadOnlyList<out T> : IWideReadOnlyCollection<T> {
    T this[long index] { get; }
}