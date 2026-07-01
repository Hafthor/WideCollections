namespace com.hafthor.WideCollections;

/// <summary>
/// Represents an enumerable sequence intended for wide-collection workflows.
/// </summary>
/// <typeparam name="T">The element type.</typeparam>
public interface IWideEnumerable<out T> : IEnumerable<T> {
}
