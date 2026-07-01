# WideCollections

High-performance .NET collections that support **64-bit (`long`) indexing**, allowing them to hold far more than `Array.MaxLength` (~2.1 billion) elements. Backing storage uses a segmented jagged-array structure with bit-shift indexing, so individual allocations stay within CLR limits while the collection as a whole scales to `long.MaxValue` capacity.

[![CI](https://github.com/Hafthor/WideCollections/actions/workflows/ci.yml/badge.svg)](https://github.com/Hafthor/WideCollections/actions/workflows/ci.yml)

## Why?

Standard BCL collections index with `int` and arrays cap at `Array.MaxLength`. When you genuinely need billions of elements in memory, WideCollections provides drop-in-style equivalents that index with `long` and grow past that ceiling.

## Install

```bash
dotnet add package WideCollections
```

Target framework: **net10.0**.

## Types

| Type | BCL analog | Notes |
|------|------------|-------|
| `WideArray<T>` | `T[]` | Fixed-length, segmented, `long` indexed, `Clone`/`Fill`/`AsMemory` |
| `WideList<T>` | `List<T>` | Grows dynamically; `AddRange`, `InsertRange`, `RemoveRange`, `RemoveAll`, `Reverse` |
| `WideDictionary<TKey,TValue>` | `Dictionary<,>` | Open hashing; `TryAdd`, `Compact` |
| `WideHashSet<T>` | `HashSet<T>` | Set ops, `Compact` |
| `WideSortedSet<T>` | `SortedSet<T>` | Binary search; `GetViewBetween`, `GetViewMemoryBetween` |
| `WideSortedList<TKey,TValue>` | `SortedList<,>` | Index access by key/position |
| `WideSortedDictionary<TKey,TValue>` | `SortedDictionary<,>` | Sorted key/value views |
| `WideOrderedDictionary<TKey,TValue>` | `OrderedDictionary` | Insertion-ordered, index + key access |
| `WideQueue<T>` | `Queue<T>` | Circular buffer |
| `WideStack<T>` | `Stack<T>` | |
| `WidePriorityQueue<TElement,TPriority>` | `PriorityQueue<,>` | Heap; `Contains`, `Remove` |
| `WideBitArray` | `BitArray` | `And/Or/Xor/Not/SetAll`, thread-safe set |
| `WideMemory<T>` / `WideReadOnlyMemory<T>` | `Memory<T>` | `long`-sliceable views |
| `WideString` | `string` | Immutable `long`-length char sequence; `Substring`, `Concat`, `IndexOf`, `StartsWith`/`EndsWith` |
| `WideStringBuilder` | `StringBuilder` | Mutable `long`-length char buffer; chainable `Append`/`Insert`/`Remove` |

All types live in the `com.hafthor.WideCollections` namespace.

## Quick start

```csharp
using com.hafthor.WideCollections;

// A list with more than int.MaxValue elements
var list = new WideList<long>();
for (long i = 0; i < 3_000_000_000L; i++)
    list.Add(i);

long value = list[2_500_000_000L];

// Dictionary
var dict = new WideDictionary<string, int>();
dict.TryAdd("answer", 42);

// Sorted set with range views
var set = new WideSortedSet<int>(new[] { 1, 2, 3, 4, 5 });
WideMemory<int> slice = set.GetViewMemoryBetween(2, 4); // 2,3,4

// Wide strings
WideString greeting = "hello";
WideString shout = greeting + " world";        // immutable concat
long idx = shout.IndexOf('w');                  // 6

var sb = new WideStringBuilder();
sb.Append("wide").Append('-').Append("string"); // chainable
WideString result = sb.ToWideString();          // independent snapshot
```

## LINQ-style extensions

`IWideEnumerable<T>` works with regular LINQ; `WideEnumerableExtensions` adds `long`-aware operators:

```csharp
var even = list.AsWide()
    .WhereWide((x, i) => i % 2 == 0)   // predicate receives long index
    .SelectWide((x, i) => x * 10);

long n = list.AsWide().LongCount();    // O(1) for wide collections
list.AsWide().ForEachWide((x, i) => Console.WriteLine($"{i}:{x}"));
var array = list.AsWide().ToWideArray();
```

## Building & testing

```bash
dotnet build
dotnet test
```

### Single-project layout (tests live alongside source)

This repo deliberately keeps the `*Tests.cs` files in the **same project** as the
library rather than in a separate test project. A single `IncludeTests` MSBuild
property (defaulting to `true`) controls whether tests and the MSTest dependency
are part of the build:

```bash
# Build/test as a test project (default)
dotnet test

# Produce a clean library package — tests, MSTestSettings, and the
# MSTest dependency are all excluded; result has zero dependencies
dotnet pack -p:IncludeTests=false
```

When `IncludeTests=false`, the csproj removes `**/*Tests.cs` + `MSTestSettings.cs`
from compilation and drops the MSTest `PackageReference` / `Using`, so the shipped
NuGet package contains only the library DLL and README. CI exercises both paths.

## License

[MIT](LICENSE) © Hafthor Stefansson
