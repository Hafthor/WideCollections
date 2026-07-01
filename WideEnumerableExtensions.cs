using System.Collections;

namespace com.hafthor.WideCollections;

/// <summary>
/// Provides LINQ-like extension methods for <see cref="IWideEnumerable{T}"/>.
/// </summary>
public static class WideEnumerableExtensions {
    /// <summary>
    /// Wraps a standard enumerable in an <see cref="IWideEnumerable{T}"/> adapter.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <returns>An <see cref="IWideEnumerable{T}"/> over <paramref name="source"/>.</returns>
    public static IWideEnumerable<T> AsWide<T>(this IEnumerable<T> source) {
        ArgumentNullException.ThrowIfNull(source);
        return source as IWideEnumerable<T> ?? new WideEnumerableAdapter<T>(source);
    }

    /// <summary>
    /// Filters a sequence based on an element and its long-based index.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="predicate">The predicate to apply to each element and index.</param>
    /// <returns>A filtered wide sequence.</returns>
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

    /// <summary>
    /// Projects each element of a sequence into a new form using an element and long-based index.
    /// </summary>
    /// <typeparam name="TSource">The source element type.</typeparam>
    /// <typeparam name="TResult">The projected element type.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="selector">The projection function.</param>
    /// <returns>A projected wide sequence.</returns>
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

    /// <summary>
    /// Returns the number of elements in a wide sequence.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <returns>The long-based count of elements.</returns>
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

    /// <summary>
    /// Returns the element at a specified long-based index in a sequence.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="index">The zero-based long index.</param>
    /// <returns>The element at <paramref name="index"/>.</returns>
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

    /// <summary>
    /// Bypasses a specified number of elements in a sequence and returns the remaining elements.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="count">The number of elements to skip.</param>
    /// <returns>A sequence that contains elements after the skipped range.</returns>
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

    /// <summary>
    /// Returns a specified number of contiguous elements from the start of a sequence.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="count">The number of elements to take.</param>
    /// <returns>A sequence containing up to <paramref name="count"/> elements.</returns>
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

    /// <summary>
    /// Returns distinct elements from a sequence using an optional equality comparer.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="comparer">The comparer used to compare elements, or <see langword="null"/> for default.</param>
    /// <returns>A sequence of distinct elements.</returns>
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

    /// <summary>
    /// Materializes a wide sequence into a <see cref="WideList{T}"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <returns>A new list containing the source elements.</returns>
    public static WideList<T> ToWideList<T>(this IWideEnumerable<T> source) {
        ArgumentNullException.ThrowIfNull(source);
        WideList<T> list = new();
        foreach (T item in source)
            list.Add(item);
        return list;
    }

    /// <summary>
    /// Materializes a wide sequence into a <see cref="WideArray{T}"/>.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <returns>A new wide array containing the source elements.</returns>
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

    /// <summary>
    /// Materializes a wide sequence into a <see cref="WideHashSet{T}"/> using an optional comparer.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="comparer">The comparer used to compare elements, or <see langword="null"/> for default.</param>
    /// <returns>A new hash set containing source elements.</returns>
    public static WideHashSet<T> ToWideHashSet<T>(
        this IWideEnumerable<T> source,
        IEqualityComparer<T> comparer = null) {
        ArgumentNullException.ThrowIfNull(source);
        WideHashSet<T> set = comparer is null ? new() : new(comparer);
        foreach (T item in source)
            set.Add(item);
        return set;
    }
    
    /// <summary>
    /// Executes an action for each element in a sequence with its long-based index.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The source sequence.</param>
    /// <param name="action">The action to run for each element and index.</param>
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
