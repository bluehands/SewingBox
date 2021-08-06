using System;
using System.Threading;

namespace ParallelAsyncExplorations
{
    public class Item
    {
        public int Nr { get; }
        public DateTime CreateDate { get; }
        public string SourceInfo { get; }

        public Item(int nr)
        {
            Nr = nr;
            CreateDate = DateTime.Now;
            SourceInfo = $"Nr {Nr:000}. Produced by thread {Thread.CurrentThread.ManagedThreadId:000} at {CreateDate:HH:mm:ss.fff}";
            Console.WriteLine(SourceInfo);
        }

        public override string ToString()
        {
            return SourceInfo;
        }

        protected bool Equals(Item other)
        {
            return Nr == other.Nr;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Item) obj);
        }

        public override int GetHashCode()
        {
            return Nr;
        }
    }
}