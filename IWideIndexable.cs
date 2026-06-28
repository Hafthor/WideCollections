namespace WideCollections;

/// <summary>
/// Represents a mutable long-indexed sequence of elements.
/// </summary>
public interface IWideIndexable<T> : IWideReadOnlyIndexable<T> {
    new T this[long index] { get; set; }
}
