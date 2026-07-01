using System.Collections;

namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a mutable collection that supports more than <see cref="int.MaxValue"/> elements.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public interface IWideCollection<T> : IEnumerable<T> {
    /// <summary>
    /// Gets the number of elements contained in the collection.
    /// </summary>
    long Count { get; }

    /// <summary>
    /// Gets a value indicating whether the collection is read-only.
    /// </summary>
    bool IsReadOnly { get; }

    /// <summary>
    /// Adds an item to the collection.
    /// </summary>
    /// <param name="item">The item to add.</param>
    void Add(T item);

    /// <summary>
    /// Removes all items from the collection.
    /// </summary>
    void Clear();

    /// <summary>
    /// Determines whether the collection contains a specific item.
    /// </summary>
    /// <param name="item">The item to locate.</param>
    /// <returns><see langword="true"/> if found; otherwise, <see langword="false"/>.</returns>
    bool Contains(T item);

    /// <summary>
    /// Copies the elements of the collection to a destination <see cref="WideArray{T}"/> starting at the given index.
    /// </summary>
    /// <param name="array">The destination array.</param>
    /// <param name="arrayIndex">The zero-based start index in <paramref name="array"/>.</param>
    void CopyTo(WideArray<T> array, long arrayIndex);

    /// <summary>
    /// Removes the first occurrence of a specific item from the collection.
    /// </summary>
    /// <param name="item">The item to remove.</param>
    /// <returns><see langword="true"/> if an item was removed; otherwise, <see langword="false"/>.</returns>
    bool Remove(T item);
}

/// <summary>
/// Represents a non-generic collection that supports more than <see cref="int.MaxValue"/> elements.
/// </summary>
public interface IWideCollection : IEnumerable {
    /// <summary>
    /// Gets the number of elements contained in the collection.
    /// </summary>
    long Count { get; }

    /// <summary>
    /// Gets an object that can be used to synchronize access to the collection.
    /// </summary>
    object SyncRoot { get; }

    /// <summary>
    /// Gets a value indicating whether access to the collection is synchronized.
    /// </summary>
    bool IsSynchronized { get; }
}