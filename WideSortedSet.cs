using System.Collections;

namespace WideCollections;

public class WideSortedSet<T> : IWideSet<T>, IWideCollection, IWideReadOnlySet<T>, ICompactable {
    private readonly WideList<T> _items;
    private readonly IComparer<T> _comparer;

    public WideSortedSet() : this((IComparer<T>)null) { }

    public WideSortedSet(IComparer<T> comparer) {
        _items = new WideList<T>();
        _comparer = comparer ?? Comparer<T>.Default;
        SyncRoot = new object();
    }

    public WideSortedSet(IEnumerable<T> collection) : this(collection, null) { }

    public WideSortedSet(IEnumerable<T> collection, IComparer<T> comparer) : this(comparer) {
        ArgumentNullException.ThrowIfNull(collection);
        UnionWith(collection);
    }

    public long Count => _items.Count;
    public bool IsReadOnly => false;
    public object SyncRoot { get; }
    public bool IsSynchronized => false;
    public IComparer<T> Comparer => _comparer;
    public T Min => Count == 0 ? default! : _items[0];
    public T Max => Count == 0 ? default! : _items[Count - 1];
    internal long InternalItemsCapacity => _items.Capacity;

    public bool Add(T item) {
        long index = FindIndex(item);
        if (index >= 0)
            return false;

        _items.Insert(~index, item);
        return true;
    }

    void IWideCollection<T>.Add(T item) => Add(item);

    public void Clear() => _items.Clear();

    public void Compact() => _items.Compact();

    public bool Contains(T item) => FindIndex(item) >= 0;

    public void CopyTo(WideArray<T> array, long arrayIndex) {
        ArgumentNullException.ThrowIfNull(array);

        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex, array.Length);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(arrayIndex + Count, array.Length);

        for (long i = 0; i < Count; i++)
            array[arrayIndex + i] = _items[i];
    }

    public bool Remove(T item) {
        long index = FindIndex(item);
        if (index < 0)
            return false;

        _items.RemoveAt(index);
        return true;
    }

    public int RemoveWhere(Predicate<T> match) {
        ArgumentNullException.ThrowIfNull(match);

        int removed = 0;
        for (long i = Count - 1; i >= 0; i--) {
            if (match(_items[i])) {
                _items.RemoveAt(i);
                removed++;
            }
        }

        return removed;
    }

    public bool TryGetValue(T equalValue, out T actualValue) {
        long index = FindIndex(equalValue);
        if (index >= 0) {
            actualValue = _items[index];
            return true;
        }

        actualValue = default!;
        return false;
    }

    public WideSortedSet<T> GetViewBetween(T lowerValue, T upperValue) {
        if (_comparer.Compare(lowerValue, upperValue) > 0)
            throw new ArgumentException("Lower value must be less than or equal to upper value.");

        WideSortedSet<T> view = new(_comparer);
        for (long i = 0; i < Count; i++) {
            T item = _items[i];
            if (_comparer.Compare(item, lowerValue) < 0)
                continue;
            if (_comparer.Compare(item, upperValue) > 0)
                break;
            view.Add(item);
        }

        return view;
    }

    public IEnumerable<T> Reverse() {
        for (long i = Count - 1; i >= 0; i--)
            yield return _items[i];
    }

    public void CopyTo(T[] array) {
        if (Count > int.MaxValue)
            throw new ArgumentException("Count exceeds array indexing range.", nameof(array));

        CopyTo(array, 0, (int)Count);
    }

    public void CopyTo(T[] array, int index) {
        if (Count > int.MaxValue)
            throw new ArgumentException("Count exceeds array indexing range.", nameof(array));

        CopyTo(array, index, (int)Count);
    }

    public void CopyTo(T[] array, int index, int count) {
        ArgumentNullException.ThrowIfNull(array);
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, array.Length - count);
        if (count > Count)
            throw new ArgumentException("Destination does not have enough space.", nameof(array));

        for (int i = 0; i < count; i++)
            array[index + i] = _items[i];
    }

    public void UnionWith(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        foreach (T item in other)
            Add(item);
    }

    public void IntersectWith(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);

        HashSet<T> otherSet = new(other, new ComparerEqualityAdapter<T>(_comparer));
        for (long i = Count - 1; i >= 0; i--) {
            if (!otherSet.Contains(_items[i]))
                _items.RemoveAt(i);
        }
    }

    public void ExceptWith(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        foreach (T item in other)
            Remove(item);
    }

    public void SymmetricExceptWith(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);

        HashSet<T> unique = new(other, new ComparerEqualityAdapter<T>(_comparer));
        foreach (T item in unique) {
            if (!Remove(item))
                Add(item);
        }
    }

    public bool IsSubsetOf(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        HashSet<T> set = new(other, new ComparerEqualityAdapter<T>(_comparer));
        for (long i = 0; i < Count; i++) {
            if (!set.Contains(_items[i]))
                return false;
        }

        return true;
    }

    public bool IsSupersetOf(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        foreach (T item in other) {
            if (!Contains(item))
                return false;
        }

        return true;
    }

    public bool IsProperSupersetOf(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        HashSet<T> set = new(other, new ComparerEqualityAdapter<T>(_comparer));
        if (Count <= set.Count)
            return false;
        foreach (T item in set) {
            if (!Contains(item))
                return false;
        }

        return true;
    }

    public bool IsProperSubsetOf(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        HashSet<T> set = new(other, new ComparerEqualityAdapter<T>(_comparer));
        if (Count >= set.Count)
            return false;
        for (long i = 0; i < Count; i++) {
            if (!set.Contains(_items[i]))
                return false;
        }

        return true;
    }

    public bool Overlaps(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        foreach (T item in other) {
            if (Contains(item))
                return true;
        }

        return false;
    }

    public bool SetEquals(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        HashSet<T> set = new(other, new ComparerEqualityAdapter<T>(_comparer));
        if (set.Count != Count)
            return false;
        for (long i = 0; i < Count; i++) {
            if (!set.Contains(_items[i]))
                return false;
        }

        return true;
    }

    public IEnumerator<T> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private long FindIndex(T item) {
        long lo = 0;
        long hi = Count - 1;
        while (lo <= hi) {
            long mid = lo + ((hi - lo) >> 1);
            int cmp = _comparer.Compare(_items[mid], item);
            if (cmp == 0)
                return mid;
            if (cmp < 0)
                lo = mid + 1;
            else
                hi = mid - 1;
        }

        return ~lo;
    }

    private sealed class ComparerEqualityAdapter<TItem> : IEqualityComparer<TItem> {
        private readonly IComparer<TItem> _cmp;

        public ComparerEqualityAdapter(IComparer<TItem> cmp) => _cmp = cmp;

        public bool Equals(TItem x, TItem y) => _cmp.Compare(x, y) == 0;

        public int GetHashCode(TItem obj) => 0;
    }
}
