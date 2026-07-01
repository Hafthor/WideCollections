using System.Collections;

namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a read-only view over a contiguous region of a long-indexed collection.
/// Like ReadOnlyMemory&lt;T&gt; but supporting long-based indexing over WideArray&lt;T&gt; and WideList&lt;T&gt;.
/// </summary>
public readonly struct WideReadOnlyMemory<T> : IWideEnumerable<T> {
    private readonly IWideReadOnlyIndexable<T> _source;
    private readonly long _start;

    /// <summary>
    /// Gets the number of elements in this memory region.
    /// </summary>
    public long Length { get; }

    /// <summary>
    /// Gets a value indicating whether this memory region is empty.
    /// </summary>
    public bool IsEmpty => Length == 0;

    /// <summary>
    /// Gets an empty <see cref="WideReadOnlyMemory{T}"/> instance.
    /// </summary>
    public static WideReadOnlyMemory<T> Empty => default;

    /// <summary>
    /// Initializes a read-only memory region over an entire source indexable.
    /// </summary>
    /// <param name="source">The source storage.</param>
    public WideReadOnlyMemory(IWideReadOnlyIndexable<T> source) {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
        _start = 0;
        Length = source.Count;
    }

    /// <summary>
    /// Initializes a read-only memory region over a slice of a source indexable.
    /// </summary>
    /// <param name="source">The source storage.</param>
    /// <param name="start">The zero-based start offset.</param>
    /// <param name="length">The number of elements in the view.</param>
    public WideReadOnlyMemory(IWideReadOnlyIndexable<T> source, long start, long length) {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start + length, source.Count);

        _source = source;
        _start = start;
        Length = length;
    }

    /// <summary>
    /// Gets the element at a relative index within the memory region.
    /// </summary>
    /// <param name="index">The zero-based relative index.</param>
    public T this[long index] {
        get {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Length);
            return _source[_start + index];
        }
    }

    /// <summary>
    /// Creates a slice from this memory region starting at the specified offset.
    /// </summary>
    /// <param name="start">The zero-based start offset.</param>
    /// <returns>A sliced read-only memory region.</returns>
    public WideReadOnlyMemory<T> Slice(long start) {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start, Length);
        return new(_source, _start + start, Length - start);
    }

    /// <summary>
    /// Creates a slice from this memory region with the specified start and length.
    /// </summary>
    /// <param name="start">The zero-based start offset.</param>
    /// <param name="length">The number of elements in the slice.</param>
    /// <returns>A sliced read-only memory region.</returns>
    public WideReadOnlyMemory<T> Slice(long start, long length) {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start + length, Length);
        return new(_source, _start + start, length);
    }

    /// <summary>
    /// Copies the contents of this memory region into a new <see cref="WideArray{T}"/>.
    /// </summary>
    /// <returns>A new array containing the copied elements.</returns>
    public WideArray<T> ToWideArray() {
        WideArray<T> result = new(Length);
        CopyTo(result);
        return result;
    }

    /// <summary>
    /// Copies the contents of this memory region into the destination array.
    /// </summary>
    /// <param name="destination">The destination array.</param>
    public void CopyTo(WideArray<T> destination) {
        ArgumentNullException.ThrowIfNull(destination);
        if (!TryCopyTo(destination))
            throw new ArgumentException("Destination is too short.", nameof(destination));
    }

    /// <summary>
    /// Attempts to copy the contents of this memory region into the destination array.
    /// </summary>
    /// <param name="destination">The destination array.</param>
    /// <returns><see langword="true"/> if the copy succeeded; otherwise, <see langword="false"/>.</returns>
    public bool TryCopyTo(WideArray<T> destination) {
        if (destination is null || destination.Length < Length)
            return false;
        WideArray<T>.BulkCopyFrom(_source, _start, destination, 0, Length);
        return true;
    }

    /// <summary>
    /// Creates a read-only memory region over the specified array.
    /// </summary>
    /// <param name="array">The source array.</param>
    public static implicit operator WideReadOnlyMemory<T>(WideArray<T> array) =>
        array is null ? Empty : new(array);

    /// <summary>
    /// Creates a read-only memory region over the specified list.
    /// </summary>
    /// <param name="list">The source list.</param>
    public static implicit operator WideReadOnlyMemory<T>(WideList<T> list) =>
        list is null ? Empty : new(list);

    /// <summary>
    /// Creates a read-only memory region over the specified writable memory.
    /// </summary>
    /// <param name="memory">The source writable memory.</param>
    public static implicit operator WideReadOnlyMemory<T>(WideMemory<T> memory) =>
        memory.IsEmpty ? Empty : new(memory.Source, memory.Start, memory.Length);

    /// <summary>
    /// Returns an enumerator for elements in this memory region.
    /// </summary>
    /// <returns>An enumerator over the elements in this view.</returns>
    public IEnumerator<T> GetEnumerator() {
        for (long i = 0; i < Length; i++)
            yield return _source[_start + i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Returns a string that represents this read-only memory instance.
    /// </summary>
    /// <returns>A diagnostic display string containing element type and length.</returns>
    public override string ToString() => $"WideReadOnlyMemory<{typeof(T).Name}>[{Length}]";
}
