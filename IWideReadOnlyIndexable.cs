namespace WideCollections;

/// <summary>
/// Represents a read-only long-indexed sequence of elements.
/// </summary>
public interface IWideReadOnlyIndexable<out T> {
    T this[long index] { get; }
    long Count { get; }
}
