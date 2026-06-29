using System.Collections;

namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a read-only view over a contiguous region of a long-indexed collection.
/// Like ReadOnlyMemory&lt;T&gt; but supporting long-based indexing over WideArray&lt;T&gt; and WideList&lt;T&gt;.
/// </summary>
public readonly struct WideReadOnlyMemory<T> : IWideEnumerable<T> {
    private readonly IWideReadOnlyIndexable<T> _source;
    private readonly long _start;

    public long Length { get; }
    public bool IsEmpty => Length == 0;

    public static WideReadOnlyMemory<T> Empty => default;

    public WideReadOnlyMemory(IWideReadOnlyIndexable<T> source) {
        ArgumentNullException.ThrowIfNull(source);
        _source = source;
        _start = 0;
        Length = source.Count;
    }

    public WideReadOnlyMemory(IWideReadOnlyIndexable<T> source, long start, long length) {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start + length, source.Count);

        _source = source;
        _start = start;
        Length = length;
    }

    public T this[long index] {
        get {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Length);
            return _source[_start + index];
        }
    }

    public WideReadOnlyMemory<T> Slice(long start) {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start, Length);
        return new(_source, _start + start, Length - start);
    }

    public WideReadOnlyMemory<T> Slice(long start, long length) {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start + length, Length);
        return new(_source, _start + start, length);
    }

    public WideArray<T> ToWideArray() {
        WideArray<T> result = new(Length);
        CopyTo(result);
        return result;
    }

    public void CopyTo(WideArray<T> destination) {
        ArgumentNullException.ThrowIfNull(destination);
        if (!TryCopyTo(destination))
            throw new ArgumentException("Destination is too short.", nameof(destination));
    }

    public bool TryCopyTo(WideArray<T> destination) {
        if (destination is null || destination.Length < Length)
            return false;
        WideArray<T>.BulkCopyFrom(_source, _start, destination, 0, Length);
        return true;
    }

    public static implicit operator WideReadOnlyMemory<T>(WideArray<T> array) =>
        array is null ? Empty : new(array);

    public static implicit operator WideReadOnlyMemory<T>(WideList<T> list) =>
        list is null ? Empty : new(list);

    public static implicit operator WideReadOnlyMemory<T>(WideMemory<T> memory) =>
        memory.IsEmpty ? Empty : new(memory.Source, memory.Start, memory.Length);

    public IEnumerator<T> GetEnumerator() {
        for (long i = 0; i < Length; i++)
            yield return _source[_start + i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => $"WideReadOnlyMemory<{typeof(T).Name}>[{Length}]";
}
