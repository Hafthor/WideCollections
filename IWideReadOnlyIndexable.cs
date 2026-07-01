namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a read-only long-indexed sequence of elements.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public interface IWideReadOnlyIndexable<out T> {
    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based element index.</param>
    T this[long index] { get; }

    /// <summary>
    /// Gets the number of elements in the sequence.
    /// </summary>
    long Count { get; }
}
