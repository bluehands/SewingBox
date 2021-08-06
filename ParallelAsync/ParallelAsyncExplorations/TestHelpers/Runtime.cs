using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace ParallelAsyncExplorations
{
    public static class Runtime
    {
        public static TimeSpan Of(Action action)
        {
            var sw = Stopwatch.StartNew();
            action();
            sw.Stop();
            return sw.Elapsed;
        }

        public static async Task<TimeSpan> OfAsync(Func<Task> action)
        {
            var sw = Stopwatch.StartNew();
            await action().ConfigureAwait(false);
            sw.Stop();
            return sw.Elapsed;
        }
    }
}