using System.Collections;

namespace WideCollections;

public static class WideEnumerableExtensions {
    public static IWideEnumerable<T> AsWide<T>(this IEnumerable<T> source) {
        ArgumentNullException.ThrowIfNull(source);
        return source as IWideEnumerable<T> ?? new WideEnumerableAdapter<T>(source);
    }

    public static IWideEnumerable<T> WhereWide<T>(this IWideEnumerable<T> source, Func<T, long, bool> predicate) {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(predicate);
        return Iterator().AsWide();

        IEnumerable<T> Iterator() {
            long index = 0;
            foreach (T item in source)
                if (predicate(item, index++))
                    yield return item;
        }
    }

    public static IWideEnumerable<TResult> SelectWide<TSource, TResult>(
        this IWideEnumerable<TSource> source,
        Func<TSource, long, TResult> selector) {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(selector);
        return Iterator().AsWide();

        IEnumerable<TResult> Iterator() {
            long index = 0;
            foreach (TSource item in source)
                yield return selector(item, index++);
        }
    }

    public static long LongCount<T>(this IWideEnumerable<T> source) {
        ArgumentNullException.ThrowIfNull(source);

        if (source is IWideReadOnlyCollection<T> wideCollection)
            return wideCollection.Count;

        if (source is WideEnumerableAdapter<T> { Source: ICollection<T> collection })
            return collection.Count;

        long count = 0;
        foreach (T _ in source)
            count++;
        return count;
    }

    public static T ElementAtLong<T>(this IWideEnumerable<T> source, long index) {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(index);

        object underlying = source is WideEnumerableAdapter<T> adapter ? adapter.Source : source;
        if (underlying is IWideReadOnlyIndexable<T> indexable) {
            if (index >= indexable.Count)
                throw new ArgumentOutOfRangeException(nameof(index));
            return indexable[index];
        }

        long current = 0;
        foreach (T item in source) {
            if (current == index)
                return item;
            current++;
        }

        throw new ArgumentOutOfRangeException(nameof(index));
    }

    public static IWideEnumerable<T> SkipLong<T>(this IWideEnumerable<T> source, long count) {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return Iterator().AsWide();

        IEnumerable<T> Iterator() {
            long skipped = 0;
            foreach (T item in source)
                if (++skipped > count)
                    yield return item;
        }
    }

    public static IWideEnumerable<T> TakeLong<T>(this IWideEnumerable<T> source, long count) {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return Iterator().AsWide();

        IEnumerable<T> Iterator() {
            long taken = 0;
            foreach (T item in source) {
                if (taken++ >= count)
                    break;
                yield return item;
            }
        }
    }

    public static IWideEnumerable<T> DistinctWide<T>(
        this IWideEnumerable<T> source,
        IEqualityComparer<T> comparer = null) {
        ArgumentNullException.ThrowIfNull(source);
        return Iterator().AsWide();

        IEnumerable<T> Iterator() {
            WideHashSet<T> seen = comparer is null ? new() : new(comparer);
            foreach (T item in source)
                if (seen.Add(item))
                    yield return item;
        }
    }

    public static WideList<T> ToWideList<T>(this IWideEnumerable<T> source) {
        ArgumentNullException.ThrowIfNull(source);
        WideList<T> list = new();
        foreach (T item in source)
            list.Add(item);
        return list;
    }

    public static WideArray<T> ToWideArray<T>(this IWideEnumerable<T> source) {
        ArgumentNullException.ThrowIfNull(source);
        object underlying = source is WideEnumerableAdapter<T> adapter ? adapter.Source : source;
        if (underlying is WideArray<T> array)
            return (WideArray<T>)array.Clone();
        if (underlying is WideList<T> list) {
            WideArray<T> result = new(list.Count, list.Items.SegmentShift);
            T[][] src = list.Items.Segments, dst = result.Segments;
            for (int i = 0; i < dst.Length; i++)
                Array.Copy(src[i], dst[i], dst[i].Length);
            return result;
        }
        // For any other source: build a temp list, compact, and return its backing array
        // directly — safe because the temp list is discarded after this call.
        WideList<T> temp = source.ToWideList();
        temp.Compact();
        return temp.Items;
    }

    public static WideHashSet<T> ToWideHashSet<T>(
        this IWideEnumerable<T> source,
        IEqualityComparer<T> comparer = null) {
        ArgumentNullException.ThrowIfNull(source);
        WideHashSet<T> set = comparer is null ? new() : new(comparer);
        foreach (T item in source)
            set.Add(item);
        return set;
    }
    
    public static void ForEachWide<T>(this IWideEnumerable<T> source, Action<T, long> action) {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(action);
        long index = 0;
        foreach (T item in source)
            action(item, index++);
    }

    private sealed class WideEnumerableAdapter<T>(IEnumerable<T> source) : IWideEnumerable<T> {
        public IEnumerable<T> Source => source;

        public IEnumerator<T> GetEnumerator() => source.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
