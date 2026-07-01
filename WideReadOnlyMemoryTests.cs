namespace com.hafthor.WideCollections;

[TestClass]
public sealed class WideReadOnlyMemoryTests {
    [TestMethod]
    public void FromWideArray_HasCorrectLengthAndValues() {
        WideArray<int> array = new(4);
        for (int i = 0; i < 4; i++) array[i] = i + 10;

        WideReadOnlyMemory<int> memory = array;

        Assert.AreEqual(4L, memory.Length);
        Assert.IsFalse(memory.IsEmpty);
        CollectionAssert.AreEqual(new[] { 10, 11, 12, 13 }, memory.ToArray());
    }

    [TestMethod]
    public void Slice_ReturnsExpectedSubrange() {
        WideArray<int> array = new(6);
        for (int i = 0; i < 6; i++) array[i] = i;

        WideReadOnlyMemory<int> slice = array.AsMemory().AsReadOnly().Slice(2, 3);

        Assert.AreEqual(3L, slice.Length);
        CollectionAssert.AreEqual(new[] { 2, 3, 4 }, slice.ToArray());
    }

    [TestMethod]
    public void Slice_OutOfRange_Throws() {
        WideReadOnlyMemory<int> memory = new WideArray<int>(3).AsMemory().AsReadOnly();

        Assert.Throws<ArgumentOutOfRangeException>(() => memory.Slice(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => memory.Slice(4));
        Assert.Throws<ArgumentOutOfRangeException>(() => memory.Slice(2, 2));
    }

    [TestMethod]
    public void Indexer_OutOfRange_Throws() {
        WideReadOnlyMemory<int> memory = new WideArray<int>(2).AsMemory().AsReadOnly();

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = memory[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = memory[2]);
    }

    [TestMethod]
    public void CopyToAndTryCopyTo_WorkAsExpected() {
        WideArray<int> array = new(3);
        array[0] = 1; array[1] = 2; array[2] = 3;
        WideReadOnlyMemory<int> memory = array;

        WideArray<int> destination = new(3);
        memory.CopyTo(destination);
        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, new[] { destination[0], destination[1], destination[2] });

        WideArray<int> small = new(2);
        Assert.IsFalse(memory.TryCopyTo(small));
        Assert.Throws<ArgumentException>(() => memory.CopyTo(small));
    }

    [TestMethod]
    public void ToWideArray_ReturnsIndependentCopy() {
        WideArray<int> array = new(3);
        array[0] = 5; array[1] = 6; array[2] = 7;
        WideReadOnlyMemory<int> memory = array;

        WideArray<int> copy = memory.ToWideArray();
        copy[0] = 99;

        Assert.AreEqual(5, array[0]);
        Assert.AreEqual(99, copy[0]);
    }

    [TestMethod]
    public void ImplicitConversions_FromWideListAndWideMemory_Work() {
        WideList<int> list = new();
        list.Add(7);
        list.Add(8);
        WideReadOnlyMemory<int> fromList = list;

        WideMemory<int> writable = list.AsMemory(1, 1);
        WideReadOnlyMemory<int> fromWideMemory = writable;

        CollectionAssert.AreEqual(new[] { 7, 8 }, fromList.ToArray());
        CollectionAssert.AreEqual(new[] { 8 }, fromWideMemory.ToArray());
    }

    [TestMethod]
    public void Empty_HasZeroLengthAndCanBeEnumerated() {
        WideReadOnlyMemory<int> empty = WideReadOnlyMemory<int>.Empty;
        Assert.IsTrue(empty.IsEmpty);
        Assert.AreEqual(0L, empty.Length);
        CollectionAssert.AreEqual(Array.Empty<int>(), empty.ToArray());
    }
}
