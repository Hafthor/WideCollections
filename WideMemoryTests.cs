namespace com.hafthor.WideCollections;

[TestClass]
public class WideMemoryTests {
    [TestMethod]
    public void WideMemory_FromWideArray_CorrectLengthAndValues() {
        WideArray<int> array = new(5);
        for (int i = 0; i < 5; i++) array[i] = i * 10;

        WideMemory<int> mem = array.AsMemory();

        Assert.AreEqual(5L, mem.Length);
        Assert.IsFalse(mem.IsEmpty);
        for (int i = 0; i < 5; i++)
            Assert.AreEqual(i * 10, mem[i]);
    }

    [TestMethod]
    public void WideMemory_FromWideList_CorrectLengthAndValues() {
        WideList<string> list = new();
        list.Add("a"); list.Add("b"); list.Add("c");

        WideMemory<string> mem = list.AsMemory();

        Assert.AreEqual(3L, mem.Length);
        Assert.AreEqual("a", mem[0]);
        Assert.AreEqual("b", mem[1]);
        Assert.AreEqual("c", mem[2]);
    }

    [TestMethod]
    public void WideMemory_Slice_CorrectSubrange() {
        WideArray<int> array = new(6);
        for (int i = 0; i < 6; i++) array[i] = i;

        WideMemory<int> slice = array.AsMemory(2, 3);

        Assert.AreEqual(3L, slice.Length);
        Assert.AreEqual(2, slice[0]);
        Assert.AreEqual(3, slice[1]);
        Assert.AreEqual(4, slice[2]);
    }

    [TestMethod]
    public void WideMemory_Slice_Chained() {
        WideArray<int> array = new(10);
        for (int i = 0; i < 10; i++) array[i] = i;

        WideMemory<int> slice = array.AsMemory().Slice(3).Slice(1, 4);

        Assert.AreEqual(4L, slice.Length);
        Assert.AreEqual(4, slice[0]);
        Assert.AreEqual(7, slice[3]);
    }

    [TestMethod]
    public void WideMemory_Write_UpdatesSource() {
        WideArray<int> array = new(4);
        for (int i = 0; i < 4; i++) array[i] = i;

        WideMemory<int> mem = array.AsMemory(1, 2);
        mem[0] = 99;
        mem[1] = 100;

        Assert.AreEqual(99, array[1]);
        Assert.AreEqual(100, array[2]);
    }

    [TestMethod]
    public void WideMemory_Fill_SetsAllElements() {
        WideArray<int> array = new(5);
        WideMemory<int> mem = array.AsMemory(1, 3);
        mem.Fill(42);

        Assert.AreEqual(0, array[0]);
        Assert.AreEqual(42, array[1]);
        Assert.AreEqual(42, array[2]);
        Assert.AreEqual(42, array[3]);
        Assert.AreEqual(0, array[4]);
    }

    [TestMethod]
    public void WideMemory_ToWideArray_CopiesElements() {
        WideArray<int> array = new(5);
        for (int i = 0; i < 5; i++) array[i] = i * 2;

        WideArray<int> copy = array.AsMemory(1, 3).ToWideArray();

        Assert.AreEqual(3L, copy.Length);
        Assert.AreEqual(2, copy[0]);
        Assert.AreEqual(4, copy[1]);
        Assert.AreEqual(6, copy[2]);
    }

    [TestMethod]
    public void WideMemory_CopyTo_TryCopyTo() {
        WideArray<int> array = new(3);
        array[0] = 10; array[1] = 20; array[2] = 30;
        WideMemory<int> mem = array.AsMemory();

        WideArray<int> dest = new(3);
        mem.CopyTo(dest);
        Assert.AreEqual(10, dest[0]);
        Assert.AreEqual(20, dest[1]);
        Assert.AreEqual(30, dest[2]);

        WideArray<int> small = new(2);
        Assert.IsFalse(mem.TryCopyTo(small));
        Assert.Throws<ArgumentException>(() => mem.CopyTo(small));
    }

    [TestMethod]
    public void WideMemory_AsReadOnly_PreventsWriteViaView() {
        WideArray<int> array = new(3);
        array[0] = 1; array[1] = 2; array[2] = 3;

        WideReadOnlyMemory<int> rom = array.AsMemory().AsReadOnly();

        Assert.AreEqual(3L, rom.Length);
        Assert.AreEqual(1, rom[0]);
        Assert.AreEqual(2, rom[1]);
        Assert.AreEqual(3, rom[2]);
    }

    [TestMethod]
    public void WideReadOnlyMemory_ImplicitFromWideMemory() {
        WideArray<int> array = new(3);
        array[0] = 5; array[1] = 6; array[2] = 7;

        WideMemory<int> mem = array.AsMemory();
        WideReadOnlyMemory<int> rom = mem;

        Assert.AreEqual(3L, rom.Length);
        Assert.AreEqual(5, rom[0]);
    }

    [TestMethod]
    public void WideMemory_IndexOutOfRange_Throws() {
        WideArray<int> array = new(3);
        WideMemory<int> mem = array.AsMemory();

        Assert.Throws<ArgumentOutOfRangeException>(() => _ = mem[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = mem[3]);
        Assert.Throws<ArgumentOutOfRangeException>(() => mem[3] = 0);
    }

    [TestMethod]
    public void WideMemory_ImplicitConversionFromArrayAndList() {
        WideArray<int> array = new(2);
        array[0] = 1; array[1] = 2;

        WideList<int> list = new();
        list.Add(3); list.Add(4);

        WideMemory<int> memFromArray = array;
        WideMemory<int> memFromList = list;

        Assert.AreEqual(2L, memFromArray.Length);
        Assert.AreEqual(1, memFromArray[0]);
        Assert.AreEqual(2L, memFromList.Length);
        Assert.AreEqual(3, memFromList[0]);
    }

    [TestMethod]
    public void WideMemory_Enumeration_YieldsSliceElements() {
        WideArray<int> array = new(5);
        for (int i = 0; i < 5; i++) array[i] = i;

        int[] result = array.AsMemory(1, 3).ToArray();

        CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result);
    }

    [TestMethod]
    public void WideMemory_Empty_IsEmpty() {
        Assert.IsTrue(WideMemory<int>.Empty.IsEmpty);
        Assert.AreEqual(0L, WideMemory<int>.Empty.Length);
        Assert.IsTrue(WideReadOnlyMemory<int>.Empty.IsEmpty);
    }

    [TestMethod]
    public void WideMemory_WideLinqIntegration() {
        WideArray<int> array = new(6);
        for (int i = 0; i < 6; i++) array[i] = i + 1;

        long count = array.AsMemory(1, 4).AsWide().LongCount(x => x > 2);

        Assert.AreEqual(3L, count); // 3, 4, 5 are > 2
    }
}
