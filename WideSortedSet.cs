using System.Collections;

namespace com.hafthor.WideCollections;

public class WideSortedSet<T> : IWideSet<T>, IWideCollection, IWideReadOnlySet<T>, ICompactable {
    private readonly WideList<T> _items;

    public WideSortedSet() : this(Comparer<T>.Default) { }

    public WideSortedSet(IComparer<T> comparer) {
        _items = new WideList<T>();
        Comparer = comparer ?? Comparer<T>.Default;
    }

    public WideSortedSet(IEnumerable<T> collection) : this(collection, Comparer<T>.Default) { }

    public WideSortedSet(IEnumerable<T> collection, IComparer<T> comparer) : this(comparer) {
        ArgumentNullException.ThrowIfNull(collection);
        UnionWith(collection);
    }

    public long Count => _items.Count;
    public bool IsReadOnly => false;
    public object SyncRoot { get; } = new();
    public bool IsSynchronized => false;
    public IComparer<T> Comparer { get; }

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

        _items.CopyTo(array, arrayIndex);
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
        WideMemory<T> viewMemory = GetViewMemoryBetween(lowerValue, upperValue);

        WideSortedSet<T> view = new(Comparer);
        foreach (T item in viewMemory)
            view._items.Add(item);

        return view;
    }
    
    public WideMemory<T> GetViewMemoryBetween(T lowerValue, T upperValue) {
        if (Comparer.Compare(lowerValue, upperValue) > 0)
            throw new ArgumentException("Lower value must be less than or equal to upper value.");

        long lowerIndex = FindIndex(lowerValue), upperIndex = FindIndex(upperValue);
        if (lowerIndex < 0)
            lowerIndex = ~lowerIndex;
        if (upperIndex < 0)
            upperIndex = ~upperIndex;

        return _items.AsMemory(lowerIndex, upperIndex - lowerIndex + 1);
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

        HashSet<T> otherSet = new(other, new ComparerEqualityAdapter<T>(Comparer));
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

        HashSet<T> unique = new(other, new ComparerEqualityAdapter<T>(Comparer));
        foreach (T item in unique) {
            if (!Remove(item))
                Add(item);
        }
    }

    public bool IsSubsetOf(IEnumerable<T> other) {
        ArgumentNullException.ThrowIfNull(other);
        HashSet<T> set = new(other, new ComparerEqualityAdapter<T>(Comparer));
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
        HashSet<T> set = new(other, new ComparerEqualityAdapter<T>(Comparer));
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
        HashSet<T> set = new(other, new ComparerEqualityAdapter<T>(Comparer));
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
        HashSet<T> set = new(other, new ComparerEqualityAdapter<T>(Comparer));
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
            int cmp = Comparer.Compare(_items[mid], item);
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
