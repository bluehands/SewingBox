using System;
using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ParallelAsyncExplorations
{
    /// <summary>
    /// Try to produce 10 items in parallel.
    /// </summary>
    [TestClass]
    public class When_producing_ten_items_in_parallel
    {
        readonly List<Item> m_Items = new List<Item>();

        const int MillisToProduceOneItem = 200;
        const int NrOfItems = 10;

        [TestMethod]
        public void Then_ten_different_items_where_produced()
        {
            ProduceItems();
            m_Items.ShouldHaveUniqueCount(NrOfItems);
        }

        [TestMethod]
        public void Then_items_where_produced_in_parallel()
        {
            var maxRuntime = NrOfItems / (double)3 * MillisToProduceOneItem;
            var runtime = Runtime.Of(ProduceItems);
            runtime.TotalMilliseconds.Should().BeLessThan(maxRuntime);
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
