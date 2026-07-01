using System.Collections;

namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a read-write view over a contiguous region of a long-indexed collection.
/// Like Memory&lt;T&gt; but supporting long-based indexing over WideArray&lt;T&gt; and WideList&lt;T&gt;.
/// </summary>
/// <remarks>
/// When backed by a WideList&lt;T&gt;, the view reflects the list's elements at the time AsMemory()
/// was called. Adding elements beyond the captured length will not be visible through this view.
/// </remarks>
public readonly struct WideMemory<T> : IWideEnumerable<T> {
    /// <summary>
    /// Gets the number of elements in this memory region.
    /// </summary>
    public long Length { get; }

    /// <summary>
    /// Gets a value indicating whether this memory region is empty.
    /// </summary>
    public bool IsEmpty => Length == 0;

    /// <summary>
    /// Gets an empty <see cref="WideMemory{T}"/> instance.
    /// </summary>
    public static WideMemory<T> Empty => default;

    internal IWideIndexable<T> Source { get; }

    internal long Start { get; }

    /// <summary>
    /// Initializes a memory region over an entire source indexable.
    /// </summary>
    /// <param name="source">The source storage.</param>
    public WideMemory(IWideIndexable<T> source) {
        ArgumentNullException.ThrowIfNull(source);
        Source = source;
        Start = 0;
        Length = source.Count;
    }

    /// <summary>
    /// Initializes a memory region over a slice of a source indexable.
    /// </summary>
    /// <param name="source">The source storage.</param>
    /// <param name="start">The zero-based start offset.</param>
    /// <param name="length">The number of elements in the view.</param>
    public WideMemory(IWideIndexable<T> source, long start, long length) {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start + length, source.Count);

        Source = source;
        Start = start;
        Length = length;
    }

    /// <summary>
    /// Gets or sets the element at a relative index within the memory region.
    /// </summary>
    /// <param name="index">The zero-based relative index.</param>
    public T this[long index] {
        get {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Length);
            return Source[Start + index];
        }
        set {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Length);
            Source[Start + index] = value;
        }
    }

    /// <summary>
    /// Creates a slice from this memory region starting at the specified offset.
    /// </summary>
    /// <param name="start">The zero-based start offset.</param>
    /// <returns>A sliced memory region.</returns>
    public WideMemory<T> Slice(long start) {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start, Length);
        return new(Source, Start + start, Length - start);
    }

    /// <summary>
    /// Creates a slice from this memory region with the specified start and length.
    /// </summary>
    /// <param name="start">The zero-based start offset.</param>
    /// <param name="length">The number of elements in the slice.</param>
    /// <returns>A sliced memory region.</returns>
    public WideMemory<T> Slice(long start, long length) {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start + length, Length);
        return new(Source, Start + start, length);
    }

    /// <summary>
    /// Creates a read-only view over this memory region.
    /// </summary>
    /// <returns>A read-only memory view over the same region.</returns>
    public WideReadOnlyMemory<T> AsReadOnly() => new(Source, Start, Length);

    /// <summary>
    /// Assigns the specified value to all elements in this memory region.
    /// </summary>
    /// <param name="value">The value to assign.</param>
    public void Fill(T value) {
        for (long i = 0; i < Length; i++)
            Source[Start + i] = value;
    }

    /// <summary>
    /// Copies the contents of this memory region into a new <see cref="WideArray{T}"/>.
    /// </summary>
    /// <returns>A new array containing the copied elements.</returns>
    public WideArray<T> ToWideArray() {
        WideArray<T> result = new(Length);
        WideArray<T>.BulkCopyFrom(Source, Start, result, 0, Length);
        return result;
    }

    /// <summary>
    /// Copies the contents of this memory region into the destination array.
    /// </summary>
    /// <param name="destination">The destination array.</param>
    public void CopyTo(WideArray<T> destination) {
        ArgumentNullException.ThrowIfNull(destination);
        if (destination.Length < Length)
            throw new ArgumentException("Destination is too short.", nameof(destination));
        WideArray<T>.BulkCopyFrom(Source, Start, destination, 0, Length);
    }

    /// <summary>
    /// Attempts to copy the contents of this memory region into the destination array.
    /// </summary>
    /// <param name="destination">The destination array.</param>
    /// <returns><see langword="true"/> if the copy succeeded; otherwise, <see langword="false"/>.</returns>
    public bool TryCopyTo(WideArray<T> destination) {
        if (destination is null || destination.Length < Length)
            return false;
        WideArray<T>.BulkCopyFrom(Source, Start, destination, 0, Length);
        return true;
    }

    /// <summary>
    /// Creates a memory region over the specified array.
    /// </summary>
    /// <param name="array">The source array.</param>
    public static implicit operator WideMemory<T>(WideArray<T> array) =>
        array is null ? Empty : new(array);

    /// <summary>
    /// Creates a memory region over the specified list.
    /// </summary>
    /// <param name="list">The source list.</param>
    public static implicit operator WideMemory<T>(WideList<T> list) =>
        list is null ? Empty : new(list);

    /// <summary>
    /// Returns an enumerator for elements in this memory region.
    /// </summary>
    /// <returns>An enumerator over the elements in this view.</returns>
    public IEnumerator<T> GetEnumerator() {
        for (long i = 0; i < Length; i++)
            yield return Source[Start + i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Returns a string that represents this memory instance.
    /// </summary>
    /// <returns>A diagnostic display string containing element type and length.</returns>
    public override string ToString() => $"WideMemory<{typeof(T).Name}>[{Length}]";
}
