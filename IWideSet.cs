namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a mutable set with wide collection semantics.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public interface IWideSet<T> : IWideCollection<T> {
    /// <summary>
    /// Adds an item to the set.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <returns><see langword="true"/> if added; otherwise, <see langword="false"/>.</returns>
    new bool Add(T item);

    /// <summary>
    /// Modifies the current set to contain all elements present in itself or the specified collection.
    /// </summary>
    void UnionWith(IEnumerable<T> other);

    /// <summary>
    /// Modifies the current set to contain only elements present in both itself and the specified collection.
    /// </summary>
    void IntersectWith(IEnumerable<T> other);

    /// <summary>
    /// Removes all elements in the specified collection from the current set.
    /// </summary>
    void ExceptWith(IEnumerable<T> other);

    /// <summary>
    /// Modifies the current set to contain elements present in either set, but not both.
    /// </summary>
    void SymmetricExceptWith(IEnumerable<T> other);

    /// <summary>
    /// Determines whether the current set is a subset of a specified collection.
    /// </summary>
    bool IsSubsetOf(IEnumerable<T> other);

    /// <summary>
    /// Determines whether the current set is a superset of a specified collection.
    /// </summary>
    bool IsSupersetOf(IEnumerable<T> other);

    /// <summary>
    /// Determines whether the current set is a proper superset of a specified collection.
    /// </summary>
    bool IsProperSupersetOf(IEnumerable<T> other);

    /// <summary>
    /// Determines whether the current set is a proper subset of a specified collection.
    /// </summary>
    bool IsProperSubsetOf(IEnumerable<T> other);

    /// <summary>
    /// Determines whether the current set overlaps with a specified collection.
    /// </summary>
    bool Overlaps(IEnumerable<T> other);

    /// <summary>
    /// Determines whether the current set and a specified collection contain the same elements.
    /// </summary>
    bool SetEquals(IEnumerable<T> other);
}