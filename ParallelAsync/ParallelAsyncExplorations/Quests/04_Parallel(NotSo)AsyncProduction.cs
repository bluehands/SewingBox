using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ParallelAsyncExplorations
{
    /// <summary>
    /// Try to produce 10 items in parallel using a different async producer.
    /// </summary>
    [TestClass]
    public class When_producing_ten_items_in_parallel_with_async_producer2
    {
        readonly List<Item> m_Items = new List<Item>();

        const int MillisToProduceOneItem = 200;
        const int NrOfItems = 10;

        [TestMethod]
        public async Task Then_ten_different_items_where_produced()
        {
            await ProduceItems().ConfigureAwait(false);
            m_Items.ShouldHaveUniqueCount(NrOfItems);
        }

        [TestMethod]
        public async Task Then_items_where_produced_in_parallel()
        {
            var runtime = await Runtime.OfAsync(ProduceItems).ConfigureAwait(false);

            const double maxRuntime = NrOfItems / (double)3 * MillisToProduceOneItem;
            runtime.TotalMilliseconds.Should().BeLessThan(maxRuntime);
        }

        static async Task<Item> Produce(int nr)
        {
            Thread.Sleep(200);
            return new Item(nr);
        }

        //TODO: Implement here. Please use Produce method to create the items :)
        async Task ProduceItems()
        {
            throw new NotImplementedException();
        }
    }
}
