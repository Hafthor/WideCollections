namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a read-only set with wide collection semantics.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public interface IWideReadOnlySet<T> : IWideReadOnlyCollection<T> {
    /// <summary>
    /// Determines whether the set contains a specific item.
    /// </summary>
    /// <param name="item">The item to locate.</param>
    /// <returns><see langword="true"/> if found; otherwise, <see langword="false"/>.</returns>
    bool Contains(T item);

    /// <summary>
    /// Determines whether the current set is a proper subset of a specified collection.
    /// </summary>
    bool IsProperSubsetOf(IEnumerable<T> other);

    /// <summary>
    /// Determines whether the current set is a proper superset of a specified collection.
    /// </summary>
    bool IsProperSupersetOf(IEnumerable<T> other);

    /// <summary>
    /// Determines whether the current set is a subset of a specified collection.
    /// </summary>
    bool IsSubsetOf(IEnumerable<T> other);

    /// <summary>
    /// Determines whether the current set is a superset of a specified collection.
    /// </summary>
    bool IsSupersetOf(IEnumerable<T> other);

    /// <summary>
    /// Determines whether the current set overlaps with a specified collection.
    /// </summary>
    bool Overlaps(IEnumerable<T> other);

    /// <summary>
    /// Determines whether the current set and a specified collection contain the same elements.
    /// </summary>
    bool SetEquals(IEnumerable<T> other);
}