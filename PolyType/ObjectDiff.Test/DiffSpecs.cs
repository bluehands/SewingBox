using PolyType.ReflectionProvider;

namespace ObjectDiff.Test;

[TestClass]
public sealed class DiffSpecs
{
    [TestMethod]
    [DynamicData(nameof(DiffPairs), DynamicDataSourceType.Method)]
    public void DiffTest(object? item, object? other, int expectedDiffCount)
    {
        var diffs = Compare.Diff(item, other, ReflectionTypeShapeProvider.Default);
        Assert.AreEqual(expectedDiffCount, diffs.Count);
    }

    public static IEnumerable<object?[]> DiffPairs()
    {
        return new DiffPair[]
        {
            //new (3, 5, 1),
            //new (3, 3, 0),
            //new (new DateTime(2000, 1, 1), new DateTime(2000, 1, 1), 0),
            //new (MyRecord.Create(), MyRecord.Create() with {Number = 5}, 1),
            //new (MyRecord.Create(), MyRecord.Create() with { Child = MyRecord.Create()}, 1),
            new (new List<int> {1,2,3}, new List<int>{1,3,2}, 2)
        }.Select(d => d.ToTestRow());
    }

    record DiffPair(object? Item, object? Other, int ExpectedDiffCount)
    {
        public object?[] ToTestRow() => [Item, Other, ExpectedDiffCount];
    }
}

record MyRecord(int Number, MyRecord? Child)
{
    public static MyRecord Create()
    {
        var child = new MyRecord(42, null);
        return new MyRecord(42, child);
    }
}