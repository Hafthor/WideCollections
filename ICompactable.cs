namespace com.hafthor.WideCollections;

/// <summary>
/// Defines a mechanism for compacting internal storage after removals.
/// </summary>
public interface ICompactable {
    /// <summary>
    /// Compacts internal storage to release unused capacity while preserving current elements.
    /// </summary>
    void Compact();
}
