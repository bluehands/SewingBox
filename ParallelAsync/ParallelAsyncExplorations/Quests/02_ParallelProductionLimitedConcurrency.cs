using System;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ParallelAsyncExplorations
{
    /// <summary>
    /// Try to produce 10 items in parallel with limited concurrency.
    /// </summary>
    [TestClass]
    public class When_producing_ten_items_in_parallel_never_producing_more_than_two_items_at_a_time
    {
        readonly List<Item> m_Items = new List<Item>();

        const int MillisToProduceOneItem = 200;
        const int NrOfItems = 10;
        const int DegreeOfParallelism = 2;

        [TestMethod]
        public void Then_ten_different_items_where_produced()
        {
            ProduceItems();
            m_Items.ShouldHaveUniqueCount(NrOfItems);
        }

        [TestMethod]
        public void Then_items_where_produced_in_parallel_with_maximum_concurrency_of_two()
        {
            var runtime = Runtime.Of(ProduceItems).TotalMilliseconds;
            const int minRuntime = NrOfItems / DegreeOfParallelism * MillisToProduceOneItem;
            const int maxRuntime = minRuntime + 2 * MillisToProduceOneItem;

            runtime.Should().BeGreaterOrEqualTo(minRuntime);
            runtime.Should().BeLessThan(maxRuntime);

            m_Items.ShouldHaveBeenProducedWithMaxConcurreny(DegreeOfParallelism, MillisToProduceOneItem);
        }

        static Item Produce(int nr)
        {
            Thread.Sleep(MillisToProduceOneItem);
            return new Item(nr);
        }

        //TODO: Implement here. Please use Produce method to create the items :)
        void ProduceItems()
        {
            throw new NotImplementedException();
        }
    }
}
