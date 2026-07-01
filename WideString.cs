using System.Collections;

namespace com.hafthor.WideCollections;

/// <summary>
/// Represents an immutable sequence of characters backed by a <see cref="WideArray{T}"/>, allowing
/// text longer than the practical length limit of a <see cref="string"/> (which cannot exceed
/// <see cref="int.MaxValue"/> characters).
/// </summary>
/// <remarks>
/// A <see cref="WideString"/> is indexed by <see cref="long"/>. Because it can hold more than
/// <see cref="int.MaxValue"/> characters, it cannot always be materialized into a <see cref="string"/>;
/// use <see cref="ToString(long, int)"/> to extract a bounded slice as a <see cref="string"/>. Operations
/// such as equality and hashing scan the entire value and are therefore O(n).
/// </remarks>
public sealed class WideString : IWideEnumerable<char>, IEquatable<WideString>, IComparable<WideString> {
    private readonly WideArray<char> _chars;

    /// <summary>
    /// Gets an empty <see cref="WideString"/>.
    /// </summary>
    public static readonly WideString Empty = new(new WideArray<char>(0), true);

    /// <summary>
    /// Initializes a new <see cref="WideString"/> that contains the characters copied from the specified string.
    /// </summary>
    /// <param name="value">The source string.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public WideString(string value) {
        ArgumentNullException.ThrowIfNull(value);
        _chars = new WideArray<char>(value.Length);
        for (int i = 0; i < value.Length; i++)
            _chars[i] = value[i];
    }

    /// <summary>
    /// Initializes a new <see cref="WideString"/> that contains the characters copied from the specified array.
    /// </summary>
    /// <param name="chars">The source characters.</param>
    /// <exception cref="ArgumentNullException"><paramref name="chars"/> is <see langword="null"/>.</exception>
    public WideString(WideArray<char> chars) {
        ArgumentNullException.ThrowIfNull(chars);
        _chars = (WideArray<char>)chars.Clone();
    }

    /// <summary>
    /// Initializes a new <see cref="WideString"/> that contains the characters copied from the specified memory region.
    /// </summary>
    /// <param name="memory">The source memory region.</param>
    public WideString(WideMemory<char> memory) => _chars = memory.ToWideArray();

    /// <summary>
    /// Initializes a new <see cref="WideString"/> that contains the characters produced by the specified sequence.
    /// </summary>
    /// <param name="chars">The source character sequence.</param>
    /// <exception cref="ArgumentNullException"><paramref name="chars"/> is <see langword="null"/>.</exception>
    public WideString(IEnumerable<char> chars) {
        ArgumentNullException.ThrowIfNull(chars);
        WideList<char> list = new();
        foreach (char c in chars)
            list.Add(c);
        _chars = list.AsMemory().ToWideArray();
    }

    /// <summary>
    /// Initializes a new <see cref="WideString"/> consisting of the specified character repeated a number of times.
    /// </summary>
    /// <param name="c">The character to repeat.</param>
    /// <param name="count">The number of times to repeat <paramref name="c"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    public WideString(char c, long count) {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        _chars = new WideArray<char>(count);
        _chars.Fill(c);
    }

    private WideString(WideArray<char> chars, bool owned) => _chars = chars;

    /// <summary>
    /// Gets the number of characters in this string.
    /// </summary>
    public long Length => _chars.Length;

    /// <summary>
    /// Gets a value indicating whether this string has no characters.
    /// </summary>
    public bool IsEmpty => _chars.Length == 0;

    /// <summary>
    /// Gets the character at the specified zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index of the character to get.</param>
    /// <returns>The character at <paramref name="index"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is outside the bounds of the string.</exception>
    public char this[long index] => _chars[index];

    /// <summary>
    /// Retrieves a substring beginning at the specified index and continuing to the end of this string.
    /// </summary>
    /// <param name="startIndex">The zero-based starting character position.</param>
    /// <returns>A new <see cref="WideString"/> containing the requested characters.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is negative or greater than <see cref="Length"/>.</exception>
    public WideString Substring(long startIndex) {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(startIndex, Length);
        return FromRange(_chars, startIndex, Length - startIndex);
    }

    /// <summary>
    /// Retrieves a substring beginning at the specified index with the specified length.
    /// </summary>
    /// <param name="startIndex">The zero-based starting character position.</param>
    /// <param name="length">The number of characters in the substring.</param>
    /// <returns>A new <see cref="WideString"/> containing the requested characters.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The requested range lies outside the bounds of the string.</exception>
    public WideString Substring(long startIndex, long length) {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(startIndex + length, Length);
        return FromRange(_chars, startIndex, length);
    }

    /// <summary>
    /// Concatenates this string with another and returns the combined result.
    /// </summary>
    /// <param name="other">The string to append.</param>
    /// <returns>A new <see cref="WideString"/> containing the characters of both strings.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="other"/> is <see langword="null"/>.</exception>
    public WideString Concat(WideString other) {
        ArgumentNullException.ThrowIfNull(other);
        WideArray<char> dest = new(Length + other.Length);
        if (Length > 0)
            WideArray<char>.BulkCopy(_chars, 0, dest, 0, Length);
        if (other.Length > 0)
            WideArray<char>.BulkCopy(other._chars, 0, dest, Length, other.Length);
        return new WideString(dest, true);
    }

    /// <summary>
    /// Returns the index of the first occurrence of the specified character.
    /// </summary>
    /// <param name="value">The character to locate.</param>
    /// <returns>The zero-based index of the character, or -1 if it is not found.</returns>
    public long IndexOf(char value) => IndexOf(value, 0);

    /// <summary>
    /// Returns the index of the first occurrence of the specified character, starting the search at the specified index.
    /// </summary>
    /// <param name="value">The character to locate.</param>
    /// <param name="startIndex">The zero-based index at which the search starts.</param>
    /// <returns>The zero-based index of the character, or -1 if it is not found.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="startIndex"/> is negative or greater than <see cref="Length"/>.</exception>
    public long IndexOf(char value, long startIndex) {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(startIndex, Length);
        for (long i = startIndex; i < Length; i++)
            if (_chars[i] == value)
                return i;
        return -1;
    }

    /// <summary>
    /// Determines whether this string contains the specified character.
    /// </summary>
    /// <param name="value">The character to locate.</param>
    /// <returns><see langword="true"/> if the character is found; otherwise <see langword="false"/>.</returns>
    public bool Contains(char value) => IndexOf(value) >= 0;

    /// <summary>
    /// Determines whether this string begins with the same characters as the specified string.
    /// </summary>
    /// <param name="value">The string to compare against the start of this string.</param>
    /// <returns><see langword="true"/> if this string starts with <paramref name="value"/>; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public bool StartsWith(WideString value) {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length > Length)
            return false;
        for (long i = 0; i < value.Length; i++)
            if (_chars[i] != value._chars[i])
                return false;
        return true;
    }

    /// <summary>
    /// Determines whether this string ends with the same characters as the specified string.
    /// </summary>
    /// <param name="value">The string to compare against the end of this string.</param>
    /// <returns><see langword="true"/> if this string ends with <paramref name="value"/>; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public bool EndsWith(WideString value) {
        ArgumentNullException.ThrowIfNull(value);
        if (value.Length > Length)
            return false;
        long offset = Length - value.Length;
        for (long i = 0; i < value.Length; i++)
            if (_chars[offset + i] != value._chars[i])
                return false;
        return true;
    }

    /// <summary>
    /// Creates a read-only memory view over the characters of this string.
    /// </summary>
    /// <returns>A <see cref="WideReadOnlyMemory{T}"/> over the characters.</returns>
    public WideReadOnlyMemory<char> AsMemory() => _chars.AsReadOnlyMemory();

    /// <summary>
    /// Determines whether this string is equal to another, comparing characters ordinally.
    /// </summary>
    /// <param name="other">The string to compare with.</param>
    /// <returns><see langword="true"/> if the strings have the same characters; otherwise <see langword="false"/>.</returns>
    public bool Equals(WideString other) {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;
        if (Length != other.Length)
            return false;
        for (long i = 0; i < Length; i++)
            if (_chars[i] != other._chars[i])
                return false;
        return true;
    }

    /// <inheritdoc />
    public override bool Equals(object obj) => obj is WideString other && Equals(other);

    /// <summary>
    /// Returns an ordinal hash code computed over all characters in this string.
    /// </summary>
    /// <returns>A hash code for this string.</returns>
    /// <remarks>This operation is O(n) because it scans every character.</remarks>
    public override int GetHashCode() {
        HashCode hash = new();
        for (long i = 0; i < Length; i++)
            hash.Add(_chars[i]);
        return hash.ToHashCode();
    }

    /// <summary>
    /// Compares this string with another using an ordinal character comparison.
    /// </summary>
    /// <param name="other">The string to compare with.</param>
    /// <returns>A negative value, zero, or a positive value indicating the relative order of the strings.</returns>
    public int CompareTo(WideString other) {
        if (other is null)
            return 1;
        long min = Math.Min(Length, other.Length);
        for (long i = 0; i < min; i++) {
            int c = _chars[i].CompareTo(other._chars[i]);
            if (c != 0)
                return c;
        }
        return Length.CompareTo(other.Length);
    }

    /// <summary>
    /// Returns the entire string as a <see cref="string"/>.
    /// </summary>
    /// <returns>A <see cref="string"/> containing every character of this instance.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Length"/> exceeds <see cref="int.MaxValue"/>, so it cannot fit in a <see cref="string"/>.</exception>
    public override string ToString() {
        if (Length > int.MaxValue)
            throw new InvalidOperationException(
                $"WideString of length {Length} exceeds the maximum length of a System.String; use ToString(start, length) to extract a slice.");
        return ToString(0, (int)Length);
    }

    /// <summary>
    /// Returns a bounded slice of this string as a <see cref="string"/>.
    /// </summary>
    /// <param name="startIndex">The zero-based starting character position.</param>
    /// <param name="length">The number of characters to include.</param>
    /// <returns>A <see cref="string"/> containing the requested characters.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The requested range lies outside the bounds of the string.</exception>
    public string ToString(long startIndex, int length) {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(startIndex + length, Length);
        char[] buffer = new char[length];
        for (int i = 0; i < length; i++)
            buffer[i] = _chars[startIndex + i];
        return new string(buffer);
    }

    /// <summary>
    /// Returns an enumerator that iterates over the characters of this string.
    /// </summary>
    /// <returns>An enumerator for the characters.</returns>
    public IEnumerator<char> GetEnumerator() {
        for (long i = 0; i < Length; i++)
            yield return _chars[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="WideString"/>.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    public static implicit operator WideString(string value) => value is null ? null : new WideString(value);

    /// <summary>
    /// Concatenates two <see cref="WideString"/> instances.
    /// </summary>
    /// <param name="left">The first string.</param>
    /// <param name="right">The second string.</param>
    /// <returns>A new <see cref="WideString"/> containing the characters of both strings.</returns>
    public static WideString operator +(WideString left, WideString right) {
        ArgumentNullException.ThrowIfNull(left);
        ArgumentNullException.ThrowIfNull(right);
        return left.Concat(right);
    }

    /// <summary>
    /// Determines whether two <see cref="WideString"/> instances have equal characters.
    /// </summary>
    /// <param name="left">The first string.</param>
    /// <param name="right">The second string.</param>
    /// <returns><see langword="true"/> if the strings are equal; otherwise <see langword="false"/>.</returns>
    public static bool operator ==(WideString left, WideString right) =>
        left is null ? right is null : left.Equals(right);

    /// <summary>
    /// Determines whether two <see cref="WideString"/> instances have different characters.
    /// </summary>
    /// <param name="left">The first string.</param>
    /// <param name="right">The second string.</param>
    /// <returns><see langword="true"/> if the strings differ; otherwise <see langword="false"/>.</returns>
    public static bool operator !=(WideString left, WideString right) => !(left == right);

    private static WideString FromRange(WideArray<char> source, long start, long length) {
        WideArray<char> dest = new(length);
        if (length > 0)
            WideArray<char>.BulkCopy(source, start, dest, 0, length);
        return new WideString(dest, true);
    }
}
