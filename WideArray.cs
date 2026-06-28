using System.Collections;

namespace WideCollections;


/// <summary>
/// A generic array that can hold more elements than Array.MaxLength by using a jagged array structure.
/// Uses 2^30 sized segments with bitwise operations for fast indexing.
/// </summary>
public class WideArray<T> : IWideCollection<T>, IWideReadOnlyCollection<T>, IWideIndexable<T>, ICloneable {
    private const int DefaultSegmentShift = 30; // 2^30 = 1,073,741,824 elements per segment

    private T[][] _segments = [];
    private long _length = 0;
    private readonly int _segmentShift;
    private readonly int _segmentSize;
    private readonly int _segmentMask;

    public long Length => _length;
    public T[][] Segments => _segments;

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

    public T Get(long index) {
        ValidateIndex(index);
        GetSegmentAndOffset(index, out int segmentIndex, out int offset);
        return _segments[segmentIndex][offset];
    }

    public void Set(long index, T value) {
        ValidateIndex(index);
        GetSegmentAndOffset(index, out int segmentIndex, out int offset);
        _segments[segmentIndex][offset] = value;
    }

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
            System.Array.Resize(ref _segments, (int)segmentsNeeded);

            if (segmentsNeeded > 0) {
                int lastSegmentSize = (int)(newLength - ((segmentsNeeded - 1) << _segmentShift));
                System.Array.Resize(ref _segments[segmentsNeeded - 1], lastSegmentSize);
            }
        } else {
            // Growing - add new segments or expand existing ones
            long currentSegments = _segments.Length;
            long segmentsNeeded = (newLength + _segmentSize - 1) >> _segmentShift;

            if (segmentsNeeded > currentSegments) {
                System.Array.Resize(ref _segments, (int)segmentsNeeded);

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

    public long Count => _length;
    public bool IsReadOnly => false;

    public void Add(T item) {
        throw new NotImplementedException();
    }

    public void Clear() {
        for (int i = 0; i < _segments.Length; i++)
            if (_segments[i] != null)
                Array.Clear(_segments[i], 0, _segments[i].Length);
    }

    public bool Contains(T findItem) {
        var comparer = EqualityComparer<T>.Default;
        foreach (T[] segment in _segments)
            foreach (T item in segment)
                if (comparer.Equals(findItem, item))
                    return true;
        return false;
    }
    
    public void CopyTo(WideArray<T> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(array.Length - arrayIndex, _length);

        for (long i = 0; i < _length; i++)
            array[arrayIndex + i] = this[i];
    }
    
    public bool Remove(T item) => throw new NotImplementedException();

    public void Fill(T value) {
        foreach (T[] segment in _segments)
            Array.Fill(segment, value);
    }

    public WideMemory<T> AsMemory() => new WideMemory<T>(this);
    public WideMemory<T> AsMemory(long start, long length) => new WideMemory<T>(this).Slice(start, length);
    public WideReadOnlyMemory<T> AsReadOnlyMemory() => new WideReadOnlyMemory<T>(this);
    public WideReadOnlyMemory<T> AsReadOnlyMemory(long start, long length) => new WideReadOnlyMemory<T>(this).Slice(start, length);

    public object Clone() {
        WideArray<T> clone = new(0, _segmentShift);
        clone.Resize(_length);
        for (int i = 0; i < _segments.Length; i++)
            Array.Copy(_segments[i], clone._segments[i], _segments[i].Length);
        return clone;
    }

    public IEnumerator<T> GetEnumerator() {
       foreach (T[] segment in _segments)
           foreach (T item in segment)
               yield return item;
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}