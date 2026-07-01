using System.Collections;

namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a mutable sequence of characters backed by a <see cref="WideList{T}"/>, allowing text to be
/// built up beyond the practical length limit of a <see cref="string"/> (which cannot exceed
/// <see cref="int.MaxValue"/> characters). This is the wide analogue of <see cref="System.Text.StringBuilder"/>.
/// </summary>
/// <remarks>
/// Content is indexed by <see cref="long"/>. Because a builder can grow beyond <see cref="int.MaxValue"/>
/// characters, use <see cref="ToWideString"/> to obtain an immutable <see cref="WideString"/>, or
/// <see cref="ToString(long, int)"/> to extract a bounded slice as a <see cref="string"/>.
/// </remarks>
public sealed class WideStringBuilder : IWideEnumerable<char> {
    private readonly WideList<char> _chars;

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="WideStringBuilder"/> class.
    /// </summary>
    public WideStringBuilder() => _chars = new WideList<char>();

    /// <summary>
    /// Initializes a new, empty instance of the <see cref="WideStringBuilder"/> class with the specified initial capacity.
    /// </summary>
    /// <param name="capacity">The initial number of characters the builder can hold before resizing.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is negative.</exception>
    public WideStringBuilder(long capacity) => _chars = new WideList<char>(capacity);

    /// <summary>
    /// Initializes a new instance of the <see cref="WideStringBuilder"/> class seeded with the characters of the specified string.
    /// </summary>
    /// <param name="value">The initial content, or <see langword="null"/> for an empty builder.</param>
    public WideStringBuilder(string value) : this() {
        if (value is not null)
            _chars.AddRange(value);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WideStringBuilder"/> class seeded with the characters of the specified wide string.
    /// </summary>
    /// <param name="value">The initial content, or <see langword="null"/> for an empty builder.</param>
    public WideStringBuilder(WideString value) : this() {
        if (value is not null)
            Append(value);
    }

    /// <summary>
    /// Gets or sets the number of characters in the builder. Setting a smaller value truncates the content;
    /// setting a larger value pads it with null characters (<c>'\0'</c>).
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is negative.</exception>
    public long Length {
        get => _chars.Count;
        set {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            long current = _chars.Count;
            if (value < current)
                _chars.RemoveRange(value, current - value);
            else if (value > current) {
                if (_chars.Capacity < value)
                    _chars.Capacity = value;
                for (long i = current; i < value; i++)
                    _chars.Add('\0');
            }
        }
    }

    /// <summary>
    /// Gets or sets the number of characters the builder can hold before its internal storage must resize.
    /// </summary>
    public long Capacity {
        get => _chars.Capacity;
        set => _chars.Capacity = value;
    }

    /// <summary>
    /// Gets or sets the character at the specified zero-based index.
    /// </summary>
    /// <param name="index">The zero-based index of the character.</param>
    /// <returns>The character at <paramref name="index"/>.</returns>
    /// <exception cref="IndexOutOfRangeException"><paramref name="index"/> is outside the bounds of the builder.</exception>
    public char this[long index] {
        get => _chars[index];
        set => _chars[index] = value;
    }

    /// <summary>
    /// Appends a single character to the end of the builder.
    /// </summary>
    /// <param name="value">The character to append.</param>
    /// <returns>This builder, to allow chaining.</returns>
    public WideStringBuilder Append(char value) {
        _chars.Add(value);
        return this;
    }

    /// <summary>
    /// Appends the specified character a number of times to the end of the builder.
    /// </summary>
    /// <param name="value">The character to append.</param>
    /// <param name="count">The number of times to append <paramref name="value"/>.</param>
    /// <returns>This builder, to allow chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is negative.</exception>
    public WideStringBuilder Append(char value, long count) {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        for (long i = 0; i < count; i++)
            _chars.Add(value);
        return this;
    }

    /// <summary>
    /// Appends the characters of the specified string to the end of the builder.
    /// </summary>
    /// <param name="value">The string to append, or <see langword="null"/> to append nothing.</param>
    /// <returns>This builder, to allow chaining.</returns>
    public WideStringBuilder Append(string value) {
        if (value is not null)
            _chars.AddRange(value);
        return this;
    }

    /// <summary>
    /// Appends the characters of the specified wide string to the end of the builder.
    /// </summary>
    /// <param name="value">The wide string to append, or <see langword="null"/> to append nothing.</param>
    /// <returns>This builder, to allow chaining.</returns>
    public WideStringBuilder Append(WideString value) {
        if (value is not null)
            for (long i = 0; i < value.Length; i++)
                _chars.Add(value[i]);
        return this;
    }

    /// <summary>
    /// Appends the current content of another builder to the end of this builder.
    /// </summary>
    /// <param name="value">The builder whose content is appended, or <see langword="null"/> to append nothing.</param>
    /// <returns>This builder, to allow chaining.</returns>
    public WideStringBuilder Append(WideStringBuilder value) {
        if (value is not null)
            for (long i = 0; i < value._chars.Count; i++)
                _chars.Add(value._chars[i]);
        return this;
    }

    /// <summary>
    /// Inserts a single character at the specified position.
    /// </summary>
    /// <param name="index">The zero-based position at which to insert.</param>
    /// <param name="value">The character to insert.</param>
    /// <returns>This builder, to allow chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or greater than <see cref="Length"/>.</exception>
    public WideStringBuilder Insert(long index, char value) {
        _chars.Insert(index, value);
        return this;
    }

    /// <summary>
    /// Inserts the characters of the specified string at the specified position.
    /// </summary>
    /// <param name="index">The zero-based position at which to insert.</param>
    /// <param name="value">The string to insert, or <see langword="null"/> to insert nothing.</param>
    /// <returns>This builder, to allow chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is negative or greater than <see cref="Length"/>.</exception>
    public WideStringBuilder Insert(long index, string value) {
        if (value is not null)
            _chars.InsertRange(index, value);
        return this;
    }

    /// <summary>
    /// Removes a range of characters from the builder.
    /// </summary>
    /// <param name="startIndex">The zero-based position at which removal begins.</param>
    /// <param name="length">The number of characters to remove.</param>
    /// <returns>This builder, to allow chaining.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The requested range lies outside the bounds of the builder.</exception>
    public WideStringBuilder Remove(long startIndex, long length) {
        _chars.RemoveRange(startIndex, length);
        return this;
    }

    /// <summary>
    /// Removes all characters from the builder.
    /// </summary>
    /// <returns>This builder, to allow chaining.</returns>
    public WideStringBuilder Clear() {
        _chars.Clear();
        return this;
    }

    /// <summary>
    /// Creates an immutable <see cref="WideString"/> containing a copy of the builder's current content.
    /// </summary>
    /// <returns>A new <see cref="WideString"/> independent of this builder.</returns>
    public WideString ToWideString() => new(_chars.AsMemory());

    /// <summary>
    /// Returns the entire content as a <see cref="string"/>.
    /// </summary>
    /// <returns>A <see cref="string"/> containing every character in the builder.</returns>
    /// <exception cref="InvalidOperationException"><see cref="Length"/> exceeds <see cref="int.MaxValue"/>, so it cannot fit in a <see cref="string"/>.</exception>
    public override string ToString() {
        if (_chars.Count > int.MaxValue)
            throw new InvalidOperationException(
                $"WideStringBuilder of length {_chars.Count} exceeds the maximum length of a System.String; use ToString(start, length) to extract a slice.");
        return ToString(0, (int)_chars.Count);
    }

    /// <summary>
    /// Returns a bounded slice of the builder's content as a <see cref="string"/>.
    /// </summary>
    /// <param name="startIndex">The zero-based starting character position.</param>
    /// <param name="length">The number of characters to include.</param>
    /// <returns>A <see cref="string"/> containing the requested characters.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The requested range lies outside the bounds of the builder.</exception>
    public string ToString(long startIndex, int length) {
        ArgumentOutOfRangeException.ThrowIfNegative(startIndex);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(startIndex + length, _chars.Count);
        char[] buffer = new char[length];
        for (int i = 0; i < length; i++)
            buffer[i] = _chars[startIndex + i];
        return new string(buffer);
    }

    /// <summary>
    /// Returns an enumerator that iterates over the characters currently in the builder.
    /// </summary>
    /// <returns>An enumerator for the characters.</returns>
    public IEnumerator<char> GetEnumerator() {
        for (long i = 0; i < _chars.Count; i++)
            yield return _chars[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
