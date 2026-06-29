namespace com.hafthor.WideCollections;

public interface IWideList<T> : IWideCollection<T>, IWideEnumerable<T> {
    T this[long index] { get; set; }
    long IndexOf(T item);
    void Insert(long index, T item);
    void RemoveAt(long index);
}

public interface IWideList : IWideCollection {
    object this[long index] { get; set; }
    long Add(object value);
    bool Contains(object value);
    void Clear();
    bool IsReadOnly { get; }
    bool IsFixedSize { get; }
    long IndexOf(object value);
    void Insert(long index, object value);
    void Remove(object value);
    void RemoveAt(long index);
}