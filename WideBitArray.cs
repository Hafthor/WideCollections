using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace com.hafthor.WideCollections;

/// <summary>
/// A bit array backed by WideArray that can hold more bits than Array.MaxLength.
/// Provides thread-safe operations for setting and resetting bits.
/// </summary>
public class WideBitArray : IWideCollection, ICloneable, ISerializable {
    private const int BitsPerLong = 64, BitsPerLongShift = 6, BitsPerLongMask = 63; // 64, log2(64), 2^6-1
    private readonly WideArray<ulong> _data;
    private long _bitLength;

    /// <summary>
    /// Gets the number of bits in the array.
    /// </summary>
    public long Length => _bitLength;

    /// <summary>
    /// Initializes a new empty instance of the <see cref="WideBitArray"/> class.
    /// </summary>
    public WideBitArray() => _data = new WideArray<ulong>();

    /// <summary>
    /// Initializes a new instance of the <see cref="WideBitArray"/> class with the specified bit length.
    /// </summary>
    /// <param name="bitCapacity">The number of bits the array contains.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="bitCapacity"/> is negative.</exception>
    public WideBitArray(long bitCapacity) {
        ArgumentOutOfRangeException.ThrowIfNegative(bitCapacity);

        long longsNeeded = (bitCapacity + BitsPerLong - 1) / BitsPerLong;
        _data = new WideArray<ulong>(longsNeeded);
        _bitLength = bitCapacity;
    }
    
    /// <summary>
    /// Gets or sets the bit at the specified zero-based index.
    /// </summary>
    /// <param name="index">The zero-based bit index.</param>
    /// <returns><see langword="true"/> if the bit is set; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is outside the bounds of the array.</exception>
    public bool this[long index] {
        get {
            ValidateIndex(index);
            GetLongAndBitOffset(index, out long longIndex, out int bitOffset);
            ulong value = _data[longIndex];
            return (value & (1UL << bitOffset)) != 0;
        }
        set {
            ValidateIndex(index);
            GetLongAndBitOffset(index, out long longIndex, out int bitOffset);
            ulong mask = 1UL << bitOffset, current = _data[longIndex];
            _data[longIndex] = value
                ? current | mask
                : current & ~mask;
        }
    }

    /// <summary>
    /// Gets the bit at the specified zero-based index.
    /// </summary>
    /// <param name="index">The zero-based bit index.</param>
    /// <returns><see langword="true"/> if the bit is set; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is outside the bounds of the array.</exception>
    public bool Get(long index) {
        ValidateIndex(index);
        GetLongAndBitOffset(index, out long longIndex, out int bitOffset);
        ulong value = _data[longIndex];
        return (value & (1UL << bitOffset)) != 0;
    }

    /// <summary>
    /// Sets the bit at the specified zero-based index to <see langword="true"/>.
    /// </summary>
    /// <param name="index">The zero-based bit index.</param>
    /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is outside the bounds of the array.</exception>
    public void Set(long index) {
        ValidateIndex(index);
        GetLongAndBitOffset(index, out long longIndex, out int bitOffset);
        ulong mask = 1UL << bitOffset;
        ulong current = _data[longIndex];
        _data[longIndex] = current | mask;
    }

    /// <summary>
    /// Sets the bit at the specified zero-based index to <see langword="false"/>.
    /// </summary>
    /// <param name="index">The zero-based bit index.</param>
    /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is outside the bounds of the array.</exception>
    public void Clear(long index) {
        ValidateIndex(index);
        GetLongAndBitOffset(index, out long longIndex, out int bitOffset);
        ulong mask = ~(1UL << bitOffset);
        ulong current = _data[longIndex];
        _data[longIndex] = current & mask;
    }

    /// <summary>
    /// Atomically sets or resets a bit in a thread-safe manner.
    /// </summary>
    /// <param name="index">The zero-based bit index.</param>
    /// <param name="value">The value to assign to the bit.</param>
    /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is outside the bounds of the array.</exception>
    public void SetBitThreadSafe(long index, bool value) {
        ValidateIndex(index);
        GetLongAndBitOffset(index, out long longIndex, out int bitOffset);
        ulong mask = 1UL << bitOffset;

        ulong original;
        ulong updated;

        do {
            original = _data[longIndex];
            if (value)
                updated = original | mask;
            else
                updated = original & ~mask;
        } while (_data.CompareExchange(longIndex, updated, original) != original);
    }

    /// <summary>
    /// Resizes the bit array to the specified bit length.
    /// </summary>
    /// <param name="newBitLength">The new number of bits in the array.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="newBitLength"/> is negative.</exception>
    public void Resize(long newBitLength) {
        ArgumentOutOfRangeException.ThrowIfNegative(newBitLength);

        if (newBitLength == _bitLength)
            return;

        long longsNeeded = newBitLength == 0 ? 0 : (newBitLength + BitsPerLong - 1) / BitsPerLong;
        _data.Resize(longsNeeded);
        _bitLength = newBitLength;
    }

    /// <summary>
    /// Clears all bits in the array.
    /// </summary>
    public void Clear() => _data.Clear();

    /// <summary>
    /// Sets every bit in the array to the specified value.
    /// </summary>
    /// <param name="value">The value to assign to each bit.</param>
    public void SetAll(bool value) {
        _data.Fill(value ? ulong.MaxValue : 0UL);
        ClearTrailingBits();
    }

    /// <summary>
    /// Applies a bitwise AND with another bit array to this instance.
    /// </summary>
    /// <param name="other">The bit array to combine with this instance.</param>
    /// <returns>This instance after the operation has been applied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="other"/> has a different length than this instance.</exception>
    public WideBitArray And(WideBitArray other) {
        ValidateSameLength(other);
        if (_data.SegmentShift != other._data.SegmentShift)
            for (long i = 0; i < _data.Length; i++)
                _data[i] &= other._data[i];
        else
            for (int segment = 0; segment < _data.Segments.Length; segment++) {
                ulong[] segmentData = _data.Segments[segment], otherSegmentData = other._data.Segments[segment];
                for (int i = 0; i < segmentData.Length; i++)
                    segmentData[i] &= otherSegmentData[i];
            }
        return this;
    }

    /// <summary>
    /// Applies a bitwise OR with another bit array to this instance.
    /// </summary>
    /// <param name="other">The bit array to combine with this instance.</param>
    /// <returns>This instance after the operation has been applied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="other"/> has a different length than this instance.</exception>
    public WideBitArray Or(WideBitArray other) {
        ValidateSameLength(other);
        if (_data.SegmentShift != other._data.SegmentShift)
            for (long i = 0; i < _data.Length; i++)
                _data[i] |= other._data[i];
        else
            for (int segment = 0; segment < _data.Segments.Length; segment++) {
                ulong[] segmentData = _data.Segments[segment], otherSegmentData = other._data.Segments[segment];
                for (int i = 0; i < segmentData.Length; i++)
                    segmentData[i] |= otherSegmentData[i];
            }
        return this;
    }

    /// <summary>
    /// Applies a bitwise exclusive OR with another bit array to this instance.
    /// </summary>
    /// <param name="other">The bit array to combine with this instance.</param>
    /// <returns>This instance after the operation has been applied.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="other"/> has a different length than this instance.</exception>
    public WideBitArray Xor(WideBitArray other) {
        ValidateSameLength(other);
        if (_data.SegmentShift != other._data.SegmentShift)
            for (long i = 0; i < _data.Length; i++)
                _data[i] ^= other._data[i];
        else
            for (int segment = 0; segment < _data.Segments.Length; segment++) {
                ulong[] segmentData = _data.Segments[segment], otherSegmentData = other._data.Segments[segment];
                for (int i = 0; i < segmentData.Length; i++)
                    segmentData[i] ^= otherSegmentData[i];
            }
        return this;
    }

    /// <summary>
    /// Inverts every bit in this instance.
    /// </summary>
    /// <returns>This instance after the operation has been applied.</returns>
    public WideBitArray Not() {
        foreach (ulong[] segmentData in _data.Segments)
            for (int i = 0; i < segmentData.Length; i++)
                segmentData[i] = ~segmentData[i];
        ClearTrailingBits();
        return this;
    }

    private void ValidateSameLength(WideBitArray other) {
        ArgumentNullException.ThrowIfNull(other);
        if (other._bitLength != _bitLength)
            throw new ArgumentException("Bit arrays must have the same length.", nameof(other));
    }

    private void ClearTrailingBits() {
        int tail = (int)(_bitLength & BitsPerLongMask);
        if (tail == 0 || _data.Length == 0)
            return;
        ulong mask = (1UL << tail) - 1;
        _data[_data.Length - 1] &= mask;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidateIndex(long index) {
        if (index < 0 || index >= _bitLength)
            throw new IndexOutOfRangeException(
                $"Index {index} is out of range for WideBitArray of length {_bitLength}.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void GetLongAndBitOffset(long index, out long longIndex, out int bitOffset) {
        longIndex = index >> BitsPerLongShift;
        bitOffset = (int)(index & BitsPerLongMask);
    }

    /// <inheritdoc />
    public IEnumerator GetEnumerator() {
        for (long i = 0; i < _bitLength; i++)
            yield return this[i];
    }
    
    /// <inheritdoc />
    public long Count => _bitLength;
    /// <inheritdoc />
    public object SyncRoot { get; } = new();
    /// <inheritdoc />
    public bool IsSynchronized => false;
    
    /// <inheritdoc />
    public object Clone() {
        WideBitArray clone = new(_bitLength);
        _data.CopyTo(clone._data, 0);
        return clone;
    }

    /// <inheritdoc />
    public void GetObjectData(SerializationInfo info, StreamingContext context) {
        ArgumentNullException.ThrowIfNull(info);
        info.AddValue(nameof(_bitLength), _bitLength);
        ulong[] data = new ulong[_data.Length];
        for (long i = 0; i < _data.Length; i++)
            data[i] = _data[i];
        info.AddValue(nameof(_data), data);
    }
}
