using System.Collections;
using System.Runtime.Serialization;

namespace com.hafthor.WideCollections;

/// <summary>
/// A bit array backed by WideArray that can hold more bits than Array.MaxLength.
/// Provides thread-safe operations for setting and resetting bits.
/// </summary>
public class WideBitArray : IWideCollection, ICloneable, ISerializable {
    private const int BitsPerLong = 64;
    private const int BitsPerLongShift = 6; // log2(64)
    private const int BitsPerLongMask = 63; // 2^6 - 1
    private readonly WideArray<ulong> _data;
    private long _bitLength;

    public long Length => _bitLength;

    public WideBitArray() => _data = new WideArray<ulong>();

    public WideBitArray(long bitCapacity) {
        ArgumentOutOfRangeException.ThrowIfNegative(bitCapacity);

        long longsNeeded = (bitCapacity + BitsPerLong - 1) / BitsPerLong;
        _data = new WideArray<ulong>(longsNeeded);
        _bitLength = bitCapacity;
    }
    
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

    public bool Get(long index) {
        ValidateIndex(index);
        GetLongAndBitOffset(index, out long longIndex, out int bitOffset);
        ulong value = _data[longIndex];
        return (value & (1UL << bitOffset)) != 0;
    }

    public void Set(long index) {
        ValidateIndex(index);
        GetLongAndBitOffset(index, out long longIndex, out int bitOffset);
        ulong mask = 1UL << bitOffset;
        ulong current = _data[longIndex];
        _data[longIndex] = current | mask;
    }

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
    public void Resize(long newBitLength) {
        ArgumentOutOfRangeException.ThrowIfNegative(newBitLength);

        if (newBitLength == _bitLength)
            return;

        long longsNeeded = newBitLength == 0 ? 0 : (newBitLength + BitsPerLong - 1) / BitsPerLong;
        _data.Resize(longsNeeded);
        _bitLength = newBitLength;
    }

    public void Clear() => _data.Clear();

    public void SetAll(bool value) {
        _data.Fill(value ? ulong.MaxValue : 0UL);
        ClearTrailingBits();
    }

    public WideBitArray And(WideBitArray other) {
        ValidateSameLength(other);
        for (long i = 0; i < _data.Length; i++)
            _data[i] &= other._data[i];
        return this;
    }

    public WideBitArray Or(WideBitArray other) {
        ValidateSameLength(other);
        for (long i = 0; i < _data.Length; i++)
            _data[i] |= other._data[i];
        return this;
    }

    public WideBitArray Xor(WideBitArray other) {
        ValidateSameLength(other);
        for (long i = 0; i < _data.Length; i++)
            _data[i] ^= other._data[i];
        return this;
    }

    public WideBitArray Not() {
        for (long i = 0; i < _data.Length; i++)
            _data[i] = ~_data[i];
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

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private void ValidateIndex(long index) {
        if (index < 0 || index >= _bitLength)
            throw new IndexOutOfRangeException(
                $"Index {index} is out of range for WideBitArray of length {_bitLength}.");
    }

    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
    private void GetLongAndBitOffset(long index, out long longIndex, out int bitOffset) {
        longIndex = index >> BitsPerLongShift;
        bitOffset = (int)(index & BitsPerLongMask);
    }

    public IEnumerator GetEnumerator() {
        for (long i = 0; i < _bitLength; i++)
            yield return this[i];
    }
    
    public long Count => _bitLength;
    public object SyncRoot { get; } = new();
    public bool IsSynchronized => false;
    
    public object Clone() {
        WideBitArray clone = new(_bitLength);
        _data.CopyTo(clone._data, 0);
        return clone;
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context) {
        ArgumentNullException.ThrowIfNull(info);
        info.AddValue(nameof(_bitLength), _bitLength);
        ulong[] data = new ulong[_data.Length];
        for (long i = 0; i < _data.Length; i++)
            data[i] = _data[i];
        info.AddValue(nameof(_data), data);
    }
}