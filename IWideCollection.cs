using System.Collections;

namespace com.hafthor.WideCollections;

public interface IWideCollection<T> : IEnumerable<T> {
    long Count { get; }
    bool IsReadOnly { get; }
    void Add(T item);
    void Clear();
    bool Contains(T item);
    void CopyTo(WideArray<T> array, long arrayIndex);
    bool Remove(T item);
}

public interface IWideCollection : IEnumerable {
    long Count { get; }
    object SyncRoot { get; }
    bool IsSynchronized { get; }
}