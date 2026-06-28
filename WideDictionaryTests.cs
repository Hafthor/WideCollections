namespace WideCollections;

[TestClass]
public sealed class WideDictionaryTests {
    [TestMethod]
    public void AddAndIndexer_GetAndSetValues() {
        WideDictionary<string, int> dictionary = new();
        dictionary.Add("a", 1);
        dictionary["b"] = 2;
        dictionary["a"] = 3;

        Assert.AreEqual(2L, dictionary.Count);
        Assert.AreEqual(3, dictionary["a"]);
        Assert.AreEqual(2, dictionary["b"]);
    }

    [TestMethod]
    public void Add_DuplicateKey_Throws() {
        WideDictionary<string, int> dictionary = new();
        dictionary.Add("dup", 1);

        Assert.Throws<ArgumentException>(() => dictionary.Add("dup", 2));
    }

    [TestMethod]
    public void ContainsTryGetAndRemove_WorkAsExpected() {
        WideDictionary<int, string> dictionary = new();
        dictionary.Add(1, "one");
        dictionary.Add(2, "two");

        Assert.IsTrue(dictionary.ContainsKey(1));
        Assert.IsTrue(dictionary.TryGetValue(2, out string value));
        Assert.AreEqual("two", value);
        Assert.IsTrue(dictionary.Remove(1));
        Assert.IsFalse(dictionary.ContainsKey(1));
        Assert.IsFalse(dictionary.Remove(1));
    }

    [TestMethod]
    public void ICollectionPairContainsAndRemove_UseKeyAndValue() {
        IWideCollection<KeyValuePair<int, string>> dictionary = new WideDictionary<int, string> {
            [1] = "one"
        };

        Assert.IsTrue(dictionary.Contains(new KeyValuePair<int, string>(1, "one")));
        Assert.IsFalse(dictionary.Contains(new KeyValuePair<int, string>(1, "ONE")));
        Assert.IsFalse(dictionary.Remove(new KeyValuePair<int, string>(1, "ONE")));
        Assert.IsTrue(dictionary.Remove(new KeyValuePair<int, string>(1, "one")));
    }

    [TestMethod]
    public void KeysAndValues_CollectionsReflectDictionary() {
        WideDictionary<int, string> dictionary = new() {
            [1] = "one",
            [2] = "two"
        };

        CollectionAssert.AreEquivalent(new[] { 1, 2 }, dictionary.Keys.ToArray());
        CollectionAssert.AreEquivalent(new[] { "one", "two" }, dictionary.Values.ToArray());
        Assert.Throws<NotSupportedException>(() => dictionary.Keys.Add(3));
        Assert.Throws<NotSupportedException>(() => dictionary.Values.Clear());
    }

    [TestMethod]
    public void CopyTo_CopiesPairsAtOffset() {
        WideDictionary<int, string> dictionary = new() {
            [1] = "one",
            [2] = "two"
        };
        WideArray<KeyValuePair<int, string>> destination = new(4);

        dictionary.CopyTo(destination, 1);

        KeyValuePair<int, string>[] copied = [destination[1], destination[2]];
        CollectionAssert.AreEquivalent(
            new[] { new KeyValuePair<int, string>(1, "one"), new KeyValuePair<int, string>(2, "two") },
            copied);
    }

    [TestMethod]
    public void NonGenericInterface_WorksForObjectKeysAndValues() {
        IWideDictionary dictionary = new WideDictionary<string, int>();
        dictionary.Add("a", 1);
        dictionary["b"] = 2;

        Assert.IsTrue(dictionary.Contains("a"));
        Assert.AreEqual(2, dictionary["b"]);
        dictionary.Remove("a");
        Assert.IsFalse(dictionary.Contains("a"));
    }

    [TestMethod]
    public void Enumerator_ThrowsWhenDictionaryModified() {
        WideDictionary<int, int> dictionary = new() {
            [1] = 10,
            [2] = 20
        };

        using var enumerator = dictionary.GetEnumerator();
        Assert.IsTrue(enumerator.MoveNext());
        dictionary.Add(3, 30);

        Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
    }

    [TestMethod]
    public void Compact_AfterRemovingMostItems_ShrinksBackingStorage() {
        WideDictionary<int, int> dictionary = new();
        for (int i = 0; i < 30; i++)
            dictionary.Add(i, i * 10);

        for (int i = 0; i < 25; i++)
            dictionary.Remove(i);

        long before = dictionary.InternalEntriesLength;
        dictionary.Compact();
        long after = dictionary.InternalEntriesLength;

        Assert.IsLessThan(before, after);
        CollectionAssert.AreEquivalent(new[] { 25, 26, 27, 28, 29 }, dictionary.Keys.OrderBy(x => x).ToArray());
    }
}
