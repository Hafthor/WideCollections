using System.Collections;

namespace WideCollections;

/// <summary>
/// Base class for key/value collection views. Subclasses implement abstract methods to handle
/// collection-specific element extraction (e.g., filtering deleted entries vs. direct indexing).
/// </summary>
internal abstract class WideKeyValueCollectionBase<T>(object syncRoot) : IWideCollection<T>, IWideCollection {
    public abstract long Count { get; }
    public object SyncRoot { get; } = syncRoot;
    public bool IsReadOnly => true;
    public bool IsSynchronized => false;

    public abstract bool Contains(T item);
    
    protected abstract T GetElementAt(long index);

    public virtual void CopyTo(WideArray<T> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
        ArgumentOutOfRangeException.ThrowIfLessThan(array.Length - arrayIndex, Count);

        for (long i = 0; i < Count; i++)
            array[arrayIndex + i] = GetElementAt(i);
    }

    public void Add(T item) => throw new NotSupportedException("Collection is read-only.");
    public bool Remove(T item) => throw new NotSupportedException("Collection is read-only.");
    public void Clear() => throw new NotSupportedException("Collection is read-only.");

    public virtual IEnumerator<T> GetEnumerator() {
        for (long i = 0; i < Count; i++)
            yield return GetElementAt(i);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
