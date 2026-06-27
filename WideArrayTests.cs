namespace WideCollections;

[TestClass]
public sealed class WideArrayTests {
    [TestMethod]
    public void ConstructorAndIndexer_WorkForValidBounds() {
        WideArray<int> array = new(3);
        array[0] = 11;
        array[1] = 22;
        array[2] = 33;

        Assert.AreEqual(3L, array.Length);
        Assert.AreEqual(11, array[0]);
        Assert.AreEqual(22, array[1]);
        Assert.AreEqual(33, array[2]);
    }

    [TestMethod]
    public void Resize_GrowAndShrink_PreservesOverlappingValues() {
        WideArray<int> array = new(2);
        array[0] = 7;
        array[1] = 8;

        array.Resize(5);
        Assert.AreEqual(5L, array.Length);
        Assert.AreEqual(7, array[0]);
        Assert.AreEqual(8, array[1]);

        array.Resize(1);
        Assert.AreEqual(1L, array.Length);
        Assert.AreEqual(7, array[0]);
        Assert.Throws<IndexOutOfRangeException>(() => _ = array[1]);
    }

    [TestMethod]
    public void CopyTo_CopiesWholeSourceAtOffset() {
        WideArray<int> source = new(3);
        source[0] = 1;
        source[1] = 2;
        source[2] = 3;
        WideArray<int> destination = new(6);

        source.CopyTo(destination, 2);

        Assert.AreEqual(0, destination[0]);
        Assert.AreEqual(0, destination[1]);
        Assert.AreEqual(1, destination[2]);
        Assert.AreEqual(2, destination[3]);
        Assert.AreEqual(3, destination[4]);
    }

    [TestMethod]
    public void Clear_RemovesValues() {
        WideArray<string> array = new(2);
        array[0] = "a";
        array[1] = "b";

        array.Clear();

        Assert.IsFalse(array.Contains("a"));
        Assert.IsFalse(array.Contains("b"));
    }

    [TestMethod]
    public void AddAndRemove_AreNotImplemented() {
        WideArray<int> array = new(1);
        Assert.Throws<NotImplementedException>(() => array.Add(1));
        Assert.Throws<NotImplementedException>(() => array.Remove(1));
    }

    [TestMethod]
    public void SmallSegmentShift_AllowsBoundaryTestingAcrossMultipleSegments() {
        WideArray<int> array = new(10, segmentShift: 2); // Segment size = 4

        Assert.AreEqual(3, array.Segments.Length);
        Assert.AreEqual(4, array.Segments[0].Length);
        Assert.AreEqual(4, array.Segments[1].Length);
        Assert.AreEqual(2, array.Segments[2].Length);

        for (int i = 0; i < 10; i++)
            array[i] = i * 10;

        for (int i = 0; i < 10; i++)
            Assert.AreEqual(i * 10, array[i]);

        array.Resize(13);
        Assert.AreEqual(4, array.Segments.Length);
        Assert.AreEqual(1, array.Segments[3].Length);
        Assert.AreEqual(90, array[9]);
    }
}
