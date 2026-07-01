using System.Collections;

namespace com.hafthor.WideCollections;


/// <summary>
/// A generic array that can hold more elements than Array.MaxLength by using a jagged array structure.
/// Uses 2^30 sized segments with bitwise operations for fast indexing.
/// </summary>
public class WideArray<T> : IWideCollection<T>, IWideReadOnlyCollection<T>, IWideIndexable<T>, ICloneable {
    private const int DefaultSegmentShift = 30; // 2^30 = 1,073,741,824 elements per segment

    private T[][] _segments = [];
    private long _length;
    private readonly int _segmentShift, _segmentSize, _segmentMask;

    /// <summary>
    /// Gets the total number of elements in the array.
    /// </summary>
    public long Length => _length;
    /// <summary>
    /// Gets the underlying jagged array of segments that back this array. Intended for advanced or test scenarios.
    /// </summary>
    public T[][] Segments => _segments;

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="WideArray{T}"/> class.
    /// </summary>
    public WideArray() : this(0) { }

    /// <summary>
    /// Initialize a WideArray with a specific capacity.
    /// </summary>
    public WideArray(long capacity) : this(capacity, DefaultSegmentShift) { }

    /// <summary>
    /// Initialize a WideArray with a specific capacity and segment shift.
    /// Intended for tests to exercise segment-boundary behavior with small segment sizes.
    /// </summary>
    internal WideArray(long capacity, int segmentShift) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(segmentShift);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(segmentShift, 30);

        _segmentShift = segmentShift;
        _segmentSize = 1 << _segmentShift;
        _segmentMask = _segmentSize - 1;

        ArgumentOutOfRangeException.ThrowIfNegative(capacity);

        if (capacity == 0)
            return;

        long segmentsNeeded = (capacity + _segmentSize - 1) >> _segmentShift;
        _segments = new T[segmentsNeeded][];

        for (long i = 0; i < segmentsNeeded; i++) {
            long segmentStart = i << _segmentShift;
            int currentSegmentSize = (int)((capacity - segmentStart) > _segmentSize
                ? _segmentSize
                : capacity - segmentStart);
            _segments[i] = new T[currentSegmentSize];
        }

        _length = capacity;
    }

    /// <summary>
    /// Returns the element at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to get.</param>
    /// <returns>The element stored at <paramref name="index"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is negative or greater than or equal to <see cref="Length"/>.</exception>
    public T Get(long index) {
        ValidateIndex(index);
        GetSegmentAndOffset(index, out int segmentIndex, out int offset);
        return _segments[segmentIndex][offset];
    }

    /// <summary>
    /// Stores a value at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the element to set.</param>
    /// <param name="value">The value to store at <paramref name="index"/>.</param>
    /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is negative or greater than or equal to <see cref="Length"/>.</exception>
    public void Set(long index, T value) {
        ValidateIndex(index);
        GetSegmentAndOffset(index, out int segmentIndex, out int offset);
        _segments[segmentIndex][offset] = value;
    }

    /// <inheritdoc />
    public T this[long index] {
        get => Get(index);
        set => Set(index, value);
    }

    /// <summary>
    /// Returns a reference to the element at the given index.
    /// Useful for atomic operations via System.Threading.Interlocked methods.
    /// </summary>
    public ref T GetRef(long index) {
        ValidateIndex(index);
        GetSegmentAndOffset(index, out int segmentIndex, out int offset);
        return ref _segments[segmentIndex][offset];
    }

    /// <summary>
    /// Extends the array to the specified length, initializing new elements to default(T).
    /// </summary>
    public void Resize(long newLength) {
        ArgumentOutOfRangeException.ThrowIfNegative(newLength);

        if (newLength == _length)
            return;

        if (newLength < _length) {
            // Shrinking - trim segments
            long segmentsNeeded = newLength == 0 ? 0 : (newLength + _segmentSize - 1) >> _segmentShift;
            Array.Resize(ref _segments, (int)segmentsNeeded);

            if (segmentsNeeded > 0) {
                int lastSegmentSize = (int)(newLength - ((segmentsNeeded - 1) << _segmentShift));
                Array.Resize(ref _segments[segmentsNeeded - 1], lastSegmentSize);
            }
        } else {
            // Growing - add new segments or expand existing ones
            long currentSegments = _segments.Length;
            long segmentsNeeded = (newLength + _segmentSize - 1) >> _segmentShift;

            if (segmentsNeeded > currentSegments) {
                Array.Resize(ref _segments, (int)segmentsNeeded);

                for (long i = currentSegments; i < segmentsNeeded; i++) {
                    long segmentStart = i << _segmentShift;
                    int currentSegmentSize = (int)((newLength - segmentStart) > _segmentSize
                        ? _segmentSize
                        : newLength - segmentStart);
                    _segments[i] = new T[currentSegmentSize];
                }
            } else if (segmentsNeeded == currentSegments && currentSegments > 0) {
                // Expand the last segment if it's not already at full size
                int lastSegmentIndex = (int)(segmentsNeeded - 1);
                int newLastSegmentSize = (int)(newLength - (lastSegmentIndex << _segmentShift));
                if (_segments[lastSegmentIndex].Length < newLastSegmentSize) {
                    Array.Resize(ref _segments[lastSegmentIndex], newLastSegmentSize);
                }
            }
        }

        _length = newLength;
    }

    private void ValidateIndex(long index) {
        if (index < 0 || index >= _length)
            throw new IndexOutOfRangeException($"Index {index} is out of range for WideArray of length {_length}.");
    }

    private void GetSegmentAndOffset(long index, out int segmentIndex, out int offset) {
        segmentIndex = (int)(index >> _segmentShift);
        offset = (int)(index & _segmentMask);
    }

    internal int SegmentShift => _segmentShift;

    /// <summary>
    /// Atomically compares and exchanges a value at the given index.
    /// Returns the original value at the index.
    /// </summary>
    public T CompareExchange(long index, T newValue, T comparand) {
        GetSegmentAndOffset(index, out int segmentIndex, out int offset);
        return Interlocked.CompareExchange(ref _segments[segmentIndex][offset], newValue, comparand);
    }

    /// <inheritdoc />
    public long Count => _length;
    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public void Add(T item) {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public void Clear() {
        foreach (T[] segment in _segments)
            if (segment != null)
                Array.Clear(segment, 0, segment.Length);
    }

    /// <inheritdoc />
    public bool Contains(T findItem) {
        var comparer = EqualityComparer<T>.Default;
        foreach (T[] segment in _segments)
            foreach (T item in segment)
                if (comparer.Equals(findItem, item))
                    return true;
        return false;
    }
    
    /// <inheritdoc />
    public void CopyTo(WideArray<T> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(array.Length - arrayIndex, _length);

        BulkCopy(this, 0, array, arrayIndex, _length);
    }

    /// <summary>
    /// Bulk-copies a contiguous range from <paramref name="source"/> into <paramref name="destination"/>
    /// using segment-aligned Array.Copy operations, even when segment sizes differ.
    /// </summary>
    internal static void BulkCopy(WideArray<T> source, long sourceIndex, WideArray<T> destination, long destinationIndex, long count) {
        while (count > 0) {
            int sSeg = (int)(sourceIndex >> source._segmentShift);
            int sOff = (int)(sourceIndex & source._segmentMask);
            int dSeg = (int)(destinationIndex >> destination._segmentShift);
            int dOff = (int)(destinationIndex & destination._segmentMask);

            int sAvail = source._segments[sSeg].Length - sOff;
            int dAvail = destination._segments[dSeg].Length - dOff;
            int chunk = (int)Math.Min(count, Math.Min(sAvail, dAvail));

            Array.Copy(source._segments[sSeg], sOff, destination._segments[dSeg], dOff, chunk);

            sourceIndex += chunk;
            destinationIndex += chunk;
            count -= chunk;
        }
    }
    
    /// <inheritdoc />
    public bool Remove(T item) => throw new NotImplementedException();

    /// <summary>
    /// Sets every element in the array to the specified value.
    /// </summary>
    /// <param name="value">The value to assign to all elements.</param>
    public void Fill(T value) {
        foreach (T[] segment in _segments)
            Array.Fill(segment, value);
    }

    /// <summary>
    /// Creates a <see cref="WideMemory{T}"/> that spans the entire array.
    /// </summary>
    /// <returns>A memory region covering all elements of this array.</returns>
    public WideMemory<T> AsMemory() => new WideMemory<T>(this);
    /// <summary>
    /// Creates a <see cref="WideMemory{T}"/> over a contiguous range of this array.
    /// </summary>
    /// <param name="start">The zero-based index at which the range begins.</param>
    /// <param name="length">The number of elements in the range.</param>
    /// <returns>A memory region covering the specified range.</returns>
    public WideMemory<T> AsMemory(long start, long length) => new WideMemory<T>(this).Slice(start, length);
    /// <summary>
    /// Creates a read-only <see cref="WideReadOnlyMemory{T}"/> that spans the entire array.
    /// </summary>
    /// <returns>A read-only memory region covering all elements of this array.</returns>
    public WideReadOnlyMemory<T> AsReadOnlyMemory() => new WideReadOnlyMemory<T>(this);
    /// <summary>
    /// Creates a read-only <see cref="WideReadOnlyMemory{T}"/> over a contiguous range of this array.
    /// </summary>
    /// <param name="start">The zero-based index at which the range begins.</param>
    /// <param name="length">The number of elements in the range.</param>
    /// <returns>A read-only memory region covering the specified range.</returns>
    public WideReadOnlyMemory<T> AsReadOnlyMemory(long start, long length) => new WideReadOnlyMemory<T>(this).Slice(start, length);

    /// <summary>
    /// Bulk-copies a contiguous range from <paramref name="source"/> into <paramref name="destination"/>.
    /// Uses segment-aligned Array.Copy when the source is a WideArray or WideList; otherwise falls back to indexing.
    /// </summary>
    internal static void BulkCopyFrom(IWideReadOnlyIndexable<T> source, long sourceIndex, WideArray<T> destination, long destinationIndex, long count) {
        WideArray<T> src = source as WideArray<T> ?? (source as WideList<T>)?.Items;
        if (src is not null) {
            BulkCopy(src, sourceIndex, destination, destinationIndex, count);
            return;
        }

        for (long i = 0; i < count; i++)
            destination[destinationIndex + i] = source[sourceIndex + i];
    }

    /// <inheritdoc />
    public object Clone() {
        WideArray<T> clone = new(0, _segmentShift);
        clone.Resize(_length);
        for (int i = 0; i < _segments.Length; i++)
            Array.Copy(_segments[i], clone._segments[i], _segments[i].Length);
        return clone;
    }

    /// <inheritdoc />
    public IEnumerator<T> GetEnumerator() {
       foreach (T[] segment in _segments)
           foreach (T item in segment)
               yield return item;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
