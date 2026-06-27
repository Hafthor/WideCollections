namespace WideCollections;

[TestClass]
public sealed class WideOrderedDictionaryTests {
    [TestMethod]
    public void AddAndEnumeration_PreserveInsertionOrder() {
        WideOrderedDictionary<int, string> dictionary = new();
        dictionary.Add(2, "two");
        dictionary.Add(1, "one");
        dictionary.Add(3, "three");

        CollectionAssert.AreEqual(new[] { 2, 1, 3 }, dictionary.Select(p => p.Key).ToArray());
    }

    [TestMethod]
    public void IndexAndKeyAccess_WorkAsExpected() {
        WideOrderedDictionary<int, string> dictionary = new();
        dictionary.Add(10, "a");
        dictionary.Add(20, "b");

        Assert.AreEqual("a", dictionary[10]);
        Assert.AreEqual(new KeyValuePair<int, string>(20, "b"), dictionary[1L]);
        dictionary.SetAt(1, "B");
        Assert.AreEqual("B", dictionary[20]);
        Assert.AreEqual(1L, dictionary.IndexOf(20));
    }

    [TestMethod]
    public void InsertAndRemoveAt_UpdateOrderAndLookups() {
        WideOrderedDictionary<int, string> dictionary = new();
        dictionary.Add(1, "one");
        dictionary.Add(3, "three");
        dictionary.Insert(1, 2, "two");

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, dictionary.Select(x => x.Key).ToArray());

        dictionary.RemoveAt(0);
        CollectionAssert.AreEqual(new[] { 2, 3 }, dictionary.Select(x => x.Key).ToArray());
        Assert.IsFalse(dictionary.ContainsKey(1));
        Assert.AreEqual(0L, dictionary.IndexOf(2));
    }

    [TestMethod]
    public void GenericAndPairBasedOperations_WorkAsExpected() {
        WideOrderedDictionary<int, string> dictionary = new();
        Assert.IsTrue(dictionary.TryAdd(1, "one"));
        Assert.IsFalse(dictionary.TryAdd(1, "uno"));
        Assert.IsTrue(dictionary.ContainsValue("one"));
        Assert.IsTrue(dictionary.Contains(new KeyValuePair<int, string>(1, "one")));
        Assert.AreEqual(0L, dictionary.IndexOf(new KeyValuePair<int, string>(1, "one")));
        Assert.IsTrue(dictionary.Remove(new KeyValuePair<int, string>(1, "one")));
    }

    [TestMethod]
    public void KeysValuesAndCopyTo_WorkAsExpected() {
        WideOrderedDictionary<int, string> dictionary = new();
        dictionary.Add(1, "one");
        dictionary.Add(2, "two");
        WideArray<KeyValuePair<int, string>> destination = new(4);

        dictionary.CopyTo(destination, 1);

        CollectionAssert.AreEqual(new[] { 1, 2 }, dictionary.Keys.ToArray());
        CollectionAssert.AreEqual(new[] { "one", "two" }, dictionary.Values.ToArray());
        Assert.AreEqual(new KeyValuePair<int, string>(1, "one"), destination[1]);
        Assert.AreEqual(new KeyValuePair<int, string>(2, "two"), destination[2]);
    }

    [TestMethod]
    public void NonGenericInterfaces_WorkForDictionaryAndListContracts() {
        IWideDictionary wideDictionary = new WideOrderedDictionary<string, int>();
        wideDictionary.Add("a", 1);
        wideDictionary["b"] = 2;
        Assert.IsTrue(wideDictionary.Contains("a"));
        Assert.AreEqual(2, wideDictionary["b"]);

        IWideList wideList = (IWideList)wideDictionary;
        long index = wideList.Add(new KeyValuePair<string, int>("c", 3));
        Assert.AreEqual(2L, index);
        Assert.AreEqual(new KeyValuePair<string, int>("c", 3), (KeyValuePair<string, int>)wideList[2]);
        wideList.Remove(new KeyValuePair<string, int>("a", 1));
        Assert.IsFalse(wideDictionary.Contains("a"));
    }

    [TestMethod]
    public void CapacityMethods_WorkAsExpected() {
        WideOrderedDictionary<int, int> dictionary = new(1);
        Assert.AreEqual(1L, dictionary.Capacity);
        Assert.IsTrue(dictionary.EnsureCapacity(10) >= 10);
        dictionary.Add(1, 1);
        dictionary.TrimExcess();
        Assert.AreEqual(dictionary.Count, dictionary.Capacity);
    }

    [TestMethod]
    public void Compact_AfterRemovingMostItems_ShrinksCapacityToCount() {
        WideOrderedDictionary<int, int> dictionary = new(64);
        for (int i = 0; i < 30; i++)
            dictionary.Add(i, i);

        for (int i = 0; i < 25; i++)
            dictionary.Remove(i);

        Assert.IsTrue(dictionary.Capacity > dictionary.Count);
        dictionary.Compact();

        Assert.AreEqual(dictionary.Count, dictionary.Capacity);
        CollectionAssert.AreEqual(new[] { 25, 26, 27, 28, 29 }, dictionary.Keys.ToArray());
    }
}
