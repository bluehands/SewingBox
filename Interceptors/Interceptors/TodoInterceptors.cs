using System.Diagnostics;
using FunicularSwitch;
using FunicularSwitch.Generic;
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
        var (result, elements) = writer;
        return Writer.Write(result, elements.Add($"{nameof(ITodoRepository.GetAll)} todos took {duration.TotalMilliseconds} ms"));
    }

    [Interceptor<ITodoRepository>(nameof(ITodoRepository.GetById))]
    public static partial async Task<Writer<GenericResult<Todo, Error>, string>> GetById(
        this ITodoRepository repo,
        Guid id)
    {
        var begin = Stopwatch.GetTimestamp();
        var writer = await repo.GetById(id);
        var duration = Stopwatch.GetElapsedTime(begin);
        var (result, elements) = writer;
        return Writer.Write(result, elements.Add($"{nameof(ITodoRepository.GetById)} {id} took {duration.TotalMilliseconds} ms"));
    }

    [Interceptor<ITodoRepository>(nameof(ITodoRepository.Add))]
    public static partial async Task<Writer<GenericResult<Todo, Error>, string>> Add(
        this ITodoRepository repo,
        CreateTodoDto dto)
    {
        var begin = Stopwatch.GetTimestamp();
        var writer = await repo.Add(dto);
        var duration = Stopwatch.GetElapsedTime(begin);
        var (result, elements) = writer;
        return Writer.Write(result, elements.Add($"{nameof(ITodoRepository.Add)} todo '{dto.Title}' took {duration.TotalMilliseconds} ms"));
    }

    [Interceptor<ITodoRepository>(nameof(ITodoRepository.Update))]
    public static partial async Task<Writer<GenericResult<Todo, Error>, string>> Update(
        this ITodoRepository repo,
        Guid id,
        UpdateTodoDto dto)
    {
        var begin = Stopwatch.GetTimestamp();
        var writer = await repo.Update(id, dto);
        var duration = Stopwatch.GetElapsedTime(begin);
        var (result, elements) = writer;
        return Writer.Write(result, elements.Add($"{nameof(ITodoRepository.Update)} {id} took {duration.TotalMilliseconds} ms"));
    }

    [Interceptor<ITodoRepository>(nameof(ITodoRepository.Delete))]
    public static partial async Task<Writer<GenericResult<FunicularSwitch.Unit, Error>, string>> Delete(
        this ITodoRepository repo,
        Guid id)
    {
        var begin = Stopwatch.GetTimestamp();
        var writer = await repo.Delete(id);
        var duration = Stopwatch.GetElapsedTime(begin);
        var (result, elements) = writer;
        return Writer.Write(result, elements.Add($"{nameof(ITodoRepository.Delete)} {id} took {duration.TotalMilliseconds} ms"));
    }
}