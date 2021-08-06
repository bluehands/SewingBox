using System;

namespace ParallelAsyncExplorations
{
    public static class DateExtension
    {
        public static long TotalMilliseconds(this DateTime dateTime)
        {
            return dateTime.Ticks / TimeSpan.TicksPerMillisecond;
        }
    }
}