namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a read-only list with long-based indexing.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public interface IWideReadOnlyList<out T> : IWideReadOnlyCollection<T> {
    /// <summary>
    /// Gets the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based element index.</param>
    T this[long index] { get; }
}