using System.Collections;

namespace WideCollections;

public interface IWideEnumerable<out T> : IEnumerable<T> {
}
