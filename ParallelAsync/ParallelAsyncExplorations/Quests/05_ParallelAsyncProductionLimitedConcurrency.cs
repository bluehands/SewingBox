using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ParallelAsyncExplorations
{
    /// <summary>
    /// Try to produce 10 items in parallel using an async producer with limited concurrency.
    /// </summary>
    [TestClass]
    public class When_producing_ten_items_in_parallel_with_async_producer_never_producing_more_than_two_items_at_a_time
    {
        readonly List<Item> m_Items = new List<Item>();

        const int MillisToProduceOneItem = 200;
        const int NrOfItems = 10;
        const int DegreeOfParallelism = 2;

        [TestMethod]
        public async Task Then_ten_different_items_where_produced()
        {
            await ProduceItems().ConfigureAwait(false);
            m_Items.ShouldHaveUniqueCount(NrOfItems);
        }

        [TestMethod]
        public async Task Then_items_where_produced_in_parallel_with_maximum_concurrency_of_two()
        {
            var runtime = (await Runtime.OfAsync(ProduceItems).ConfigureAwait(false)).TotalMilliseconds;

            const int minRuntime = NrOfItems / DegreeOfParallelism * MillisToProduceOneItem;
            const int maxRuntime = minRuntime + 2 * MillisToProduceOneItem;

            runtime.Should().BeGreaterOrEqualTo(minRuntime);
            runtime.Should().BeLessThan(maxRuntime);

            m_Items.ShouldHaveBeenProducedWithMaxConcurreny(DegreeOfParallelism, MillisToProduceOneItem);
        }

        static async Task<Item> Produce(int nr)
        {
            await Task.Delay(MillisToProduceOneItem).ConfigureAwait(false);
            return new Item(nr);
        }

        //TODO: Implement here. Please use Produce method to create the items :)
        async Task ProduceItems()
        {
            throw new NotImplementedException();
        }
    }
}