using System.Collections;

namespace WideCollections;

/// <summary>
/// Represents a read-write view over a contiguous region of a long-indexed collection.
/// Like Memory&lt;T&gt; but supporting long-based indexing over WideArray&lt;T&gt; and WideList&lt;T&gt;.
/// </summary>
/// <remarks>
/// When backed by a WideList&lt;T&gt;, the view reflects the list's elements at the time AsMemory()
/// was called. Adding elements beyond the captured length will not be visible through this view.
/// </remarks>
public readonly struct WideMemory<T> : IWideEnumerable<T> {
    public long Length { get; }
    public bool IsEmpty => Length == 0;

    public static WideMemory<T> Empty => default;

    internal IWideIndexable<T> Source { get; }

    internal long Start { get; }

    public WideMemory(IWideIndexable<T> source) {
        ArgumentNullException.ThrowIfNull(source);
        Source = source;
        Start = 0;
        Length = source.Count;
    }

    public WideMemory(IWideIndexable<T> source, long start, long length) {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start + length, source.Count);

        Source = source;
        Start = start;
        Length = length;
    }

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

    public WideMemory<T> Slice(long start) {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start, Length);
        return new(Source, Start + start, Length - start);
    }

    public WideMemory<T> Slice(long start, long length) {
        ArgumentOutOfRangeException.ThrowIfNegative(start);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start + length, Length);
        return new(Source, Start + start, length);
    }

    public WideReadOnlyMemory<T> AsReadOnly() => new(Source, Start, Length);

    public void Fill(T value) {
        for (long i = 0; i < Length; i++)
            Source[Start + i] = value;
    }

    public WideArray<T> ToWideArray() {
        WideArray<T> result = new(Length);
        for (long i = 0; i < Length; i++)
            result[i] = Source[Start + i];
        return result;
    }

    public void CopyTo(WideArray<T> destination) {
        ArgumentNullException.ThrowIfNull(destination);
        if (destination.Length < Length)
            throw new ArgumentException("Destination is too short.", nameof(destination));
        for (long i = 0; i < Length; i++)
            destination[i] = Source[Start + i];
    }

    public bool TryCopyTo(WideArray<T> destination) {
        if (destination is null || destination.Length < Length)
            return false;
        for (long i = 0; i < Length; i++)
            destination[i] = Source[Start + i];
        return true;
    }

    public static implicit operator WideMemory<T>(WideArray<T> array) =>
        array is null ? Empty : new(array);

    public static implicit operator WideMemory<T>(WideList<T> list) =>
        list is null ? Empty : new(list);

    public IEnumerator<T> GetEnumerator() {
        for (long i = 0; i < Length; i++)
            yield return Source[Start + i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public override string ToString() => $"WideMemory<{typeof(T).Name}>[{Length}]";
}
