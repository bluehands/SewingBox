using System.Diagnostics;
using Splice;

namespace Interceptors;

public static partial class TodoInterceptors
{
    [Interceptor<ITodoRepository>(nameof(ITodoRepository.GetAll))]
    public static partial async Task<Writer<GenericResult<IEnumerable<Todo>, Error>, string>> GetAll(
        this ITodoRepository repo)
    {
        var begin = Stopwatch.GetTimestamp();
        var writer = await repo.GetAll();
        var duration = Stopwatch.GetElapsedTime(begin);
        return writer.Write($"{nameof(ITodoRepository.GetAll)} todos took {duration.TotalMilliseconds} ms");
    }
}