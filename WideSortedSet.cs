using System.Collections;

namespace WideCollections;

public class WideSortedSet<T> : IWideSet<T>, IWideCollection, IWideReadOnlySet<T> {
    private readonly SortedSet<T> _set;

    private WideSortedSet(SortedSet<T> set) {
        _set = set;
        SyncRoot = new object();
    }

    public WideSortedSet() {
        _set = new SortedSet<T>();
        SyncRoot = new object();
    }

    public WideSortedSet(IComparer<T> comparer) {
        _set = comparer is null ? new SortedSet<T>() : new SortedSet<T>(comparer);
        SyncRoot = new object();
    }

    public WideSortedSet(IEnumerable<T> collection) : this(collection, null) { }

    public WideSortedSet(IEnumerable<T> collection, IComparer<T> comparer) {
        ArgumentNullException.ThrowIfNull(collection);
        _set = comparer is null ? new SortedSet<T>(collection) : new SortedSet<T>(collection, comparer);
        SyncRoot = new object();
    }

    public long Count => _set.Count;
    public bool IsReadOnly => false;
    public object SyncRoot { get; }
    public bool IsSynchronized => false;
    public IComparer<T> Comparer => _set.Comparer;
    public T Min => _set.Min;
    public T Max => _set.Max;

    public bool Add(T item) => _set.Add(item);

    void IWideCollection<T>.Add(T item) => Add(item);

    public void Clear() => _set.Clear();

    public bool Contains(T item) => _set.Contains(item);

    public void CopyTo(WideArray<T> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);

        if (arrayIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index cannot be negative.");

        if (arrayIndex > array.Length)
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index exceeds destination length.");

        if (array.Length - arrayIndex < Count)
            throw new ArgumentException("Destination does not have enough space.", nameof(array));

        long i = arrayIndex;
        foreach (T item in _set) {
            array[i] = item;
            i++;
        }
    }

    public bool Remove(T item) => _set.Remove(item);

    public int RemoveWhere(Predicate<T> match) {
        ArgumentNullException.ThrowIfNull(match);
        return _set.RemoveWhere(match);
    }

    public bool TryGetValue(T equalValue, out T actualValue) => _set.TryGetValue(equalValue, out actualValue);

    public WideSortedSet<T> GetViewBetween(T lowerValue, T upperValue) => new(_set.GetViewBetween(lowerValue, upperValue));

    public IEnumerable<T> Reverse() => _set.Reverse();

    public void CopyTo(T[] array) => _set.CopyTo(array);

    public void CopyTo(T[] array, int index) => _set.CopyTo(array, index);

    public void CopyTo(T[] array, int index, int count) => _set.CopyTo(array, index, count);

    public void UnionWith(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        _set.UnionWith(other);
    }

    public void IntersectWith(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        _set.IntersectWith(other);
    }

    public void ExceptWith(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        _set.ExceptWith(other);
    }

    public void SymmetricExceptWith(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        _set.SymmetricExceptWith(other);
    }

    public bool IsSubsetOf(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        return _set.IsSubsetOf(other);
    }

    public bool IsSupersetOf(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        return _set.IsSupersetOf(other);
    }

    public bool IsProperSupersetOf(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        return _set.IsProperSupersetOf(other);
    }

    public bool IsProperSubsetOf(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        return _set.IsProperSubsetOf(other);
    }

    public bool Overlaps(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        return _set.Overlaps(other);
    }

    public bool SetEquals(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        return _set.SetEquals(other);
    }

    public IEnumerator<T> GetEnumerator() => _set.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}