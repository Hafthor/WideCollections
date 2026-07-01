namespace com.hafthor.WideCollections;

/// <summary>
/// Represents a mutable long-indexed sequence of elements.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public interface IWideIndexable<T> : IWideReadOnlyIndexable<T> {
    /// <inheritdoc cref="IWideReadOnlyIndexable{T}.this[long]"/>
    new T this[long index] { get; set; }
}
