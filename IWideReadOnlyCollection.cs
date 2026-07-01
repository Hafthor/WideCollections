namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a read-only collection that supports more than <see cref="int.MaxValue"/> elements.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public interface IWideReadOnlyCollection<out T> : IEnumerable<T> {
    /// <summary>
    /// Gets the number of elements in the collection.
    /// </summary>
    long Count { get; }
}