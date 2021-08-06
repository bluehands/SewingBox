using System.Collections.Generic;
using System.Linq;
using FluentAssertions;

namespace ParallelAsyncExplorations
{
    public static class ItemAssertions
    {
        public static void ShouldHaveBeenProducedWithMaxConcurreny(this IEnumerable<Item> items, int degreeOfParalellism, int millisToProduceItem)
        {
            items.GroupBy(i => i.CreateDate.TotalMilliseconds() / millisToProduceItem)
                .All(g => g.Count() <= degreeOfParalellism)
                .Should()
                .BeTrue($"More than {degreeOfParalellism} items where created within one time slice");
        }

        public static void ShouldHaveUniqueCount(this IList<Item> items, int count)
        {
            items.Should().HaveCount(count);
            items.Distinct().Should().HaveCount(count);
        }
    }
}