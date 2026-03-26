using System.Diagnostics;
using FunicularSwitch;
using FunicularSwitch.Generic;

namespace Interceptors;

public static class WriterExtensions
{
    public static Writer<GenericResult<Unit, TError>, L> UnitOkWriter<TError, L>() => Writer.Unit<L>().Map(GenericResult.Ok<Unit, TError>);

    public static Writer<GenericResult<T, TError>, L> Write<T, TError, L>(
        this GenericResult<T, TError> result,
        Func<T, L> writeFn) =>
        result.Match(
            ok => Writer.Write(result, writeFn(ok)),
            error => Writer.Empty<GenericResult<T, TError>, L>(GenericResult.Error<T, TError>(error)));

    public static async Task<Writer<GenericResult<T, TError>, L>> Write<T, TError, L>(
        this Task<GenericResult<T, TError>> result,
        Func<T, L> writeFn) =>
        (await result).Write(writeFn);

    // select result
    [DebuggerStepThrough]
    public static Writer<GenericResult<B, TError>, L> Select<A, B, TError, L>(
        this GenericResult<A, TError> result,
        Func<A, B> t) =>
        Writer.Empty<GenericResult<B, TError>, L>(result.Map(t));

    [DebuggerStepThrough]
    public static Writer<GenericResult<B, TError>, L> Select<A, B, TError, L>(
        this Writer<GenericResult<A, TError>, L> writer,
        Func<A, B> t) =>
        writer.Bind(ar => ar.Select<A, B, TError, L>(t));

    [DebuggerStepThrough]
    public static async Task<Writer<GenericResult<B, TError>, L>> Select<A, B, TError, L>(
        this Task<GenericResult<A, TError>> result,
        Func<A, B> t) =>
        Writer.Empty<GenericResult<B, TError>, L>((await result).Map(t));

    [DebuggerStepThrough]
    public static async Task<Writer<GenericResult<B, TError>, L>> Select<A, B, TError, L>(
        this Task<Writer<GenericResult<A, TError>, L>> writer,
        Func<A, B> t) =>
        (await writer).Bind(ar => ar.Select<A, B, TError, L>(t));

    // bind result
    [DebuggerStepThrough]
    public static Writer<GenericResult<B, TError>, L> Bind<A, B, TError, L>(
        this Writer<GenericResult<A, TError>, L> writer,
        Func<A, GenericResult<B, TError>> f) =>
        writer.Bind(ar => Writer.Empty<GenericResult<B, TError>, L>(ar.Bind(f)));

    [DebuggerStepThrough]
    public static Writer<GenericResult<B, TError>, L> Bind<A, B, TError, L>(
        this Writer<GenericResult<A, TError>, L> writer,
        Func<A, Writer<GenericResult<B, TError>, L>> f) =>
        writer.Bind(ar => ar.Match(f, err => Writer.Empty<GenericResult<B, TError>, L>(GenericResult.Error<B, TError>(err))));

    // async bind writer
    [DebuggerStepThrough]
    public static async Task<Writer<GenericResult<B, TError>, L>> Bind<A, B, TError, L>(
        this Task<Writer<GenericResult<A, TError>, L>> writer,
        Func<A, Task<Writer<GenericResult<B, TError>, L>>> f) =>
        await (await writer).Bind(ar => ar.Match(f, err => Writer.Empty<GenericResult<B, TError>, L>(GenericResult.Error<B, TError>(err))));

    [DebuggerStepThrough]
    public static async Task<Writer<GenericResult<B, TError>, L>> Bind<A, B, TError, L>(
        this Task<Writer<GenericResult<A, TError>, L>> writer,
        Func<A, Writer<GenericResult<B, TError>, L>> f) =>
        (await writer).Bind(res => res.Match(f,
            error => Writer.Empty<GenericResult<B, TError>, L>(GenericResult.Error<B, TError>(error))));

    [DebuggerStepThrough]
    public static async Task<Writer<GenericResult<B, TError>, L>> Bind<A, B, TError, L>(
        this Writer<GenericResult<A, TError>, L> writer,
        Func<A, Task<Writer<GenericResult<B, TError>, L>>> f) =>
        await writer.Bind(res => res.Match(f,
            error => Writer.Empty<GenericResult<B, TError>, L>(GenericResult.Error<B, TError>(error))));

    // async bind result
    [DebuggerStepThrough]
    public static async Task<Writer<GenericResult<B, TError>, L>> Bind<A, B, TError, L>(
        this Task<Writer<GenericResult<A, TError>, L>> writer,
        Func<A, Task<GenericResult<B, TError>>> f) =>
        await writer.Bind(async a => Writer.Empty<GenericResult<B, TError>, L>(await f(a)));

    [DebuggerStepThrough]
    public static async Task<Writer<GenericResult<B, TError>, L>> Bind<A, B, TError, L>(
        this Task<Writer<GenericResult<A, TError>, L>> writer,
        Func<A, GenericResult<B, TError>> f) =>
        await writer.Bind(a => Writer.Empty<GenericResult<B, TError>, L>(f(a)));

    [DebuggerStepThrough]
    public static async Task<Writer<GenericResult<B, TError>, L>> Bind<A, B, TError, L>(
        this Writer<GenericResult<A, TError>, L> writer,
        Func<A, Task<GenericResult<B, TError>>> f) =>
        await writer.Bind(async a => Writer.Empty<GenericResult<B, TError>, L>(await f(a)));

    // select result
    [DebuggerStepThrough]
    public static Writer<GenericResult<C, TError>, L> SelectMany<A, B, C, TError, L>(
        this Writer<GenericResult<A, TError>, L> writer,
        Func<A, GenericResult<B, TError>> f,
        Func<A, B, C> resultSelector) =>
        writer.Bind(a => f(a).Map(b => resultSelector(a, b)));

    // select writer
    [DebuggerStepThrough]
    public static Writer<GenericResult<C, TError>, L> SelectMany<A, B, C, TError, L>(
        this Writer<GenericResult<A, TError>, L> writer,
        Func<A, Writer<GenericResult<B, TError>, L>> f,
        Func<A, B, C> resultSelector) =>
        writer.Bind(a => f(a).Map(bs => bs.Map(b => resultSelector(a, b))));

    public static Writer<GenericResult<C, TError>, L> SelectMany<A, B, C, TError, L>(
        this GenericResult<A, TError> result,
        Func<A, GenericResult<B, TError>> f,
        Func<A, B, C> resultSelector) =>
        Writer.Empty<GenericResult<C, TError>, L>(result.Bind(a => f(a).Map(b => resultSelector(a, b))));
    
    public static Writer<GenericResult<C, TError>, L> SelectMany<A, B, C, TError, L>(
        this GenericResult<A, TError> result,
        Func<A, Writer<GenericResult<B, TError>, L>> f,
        Func<A, B, C> resultSelector) =>
        result.Match<Writer<GenericResult<C, TError>, L>>(
            a => f(a).Map(br => br.Map(b => resultSelector(a, b))),
            error => Writer.Empty<GenericResult<C, TError>, L>(GenericResult.Error<C, TError>(error)));

    // async select result
    [DebuggerStepThrough]
    public static Task<Writer<GenericResult<C, TError>, L>> SelectMany<A, B, C, TError, L>(
        this Task<Writer<GenericResult<A, TError>, L>> writer,
        Func<A, Task<GenericResult<B, TError>>> f,
        Func<A, B, C> resultSelector) =>
        writer.Bind(async a => (await f(a)).Map(b => resultSelector(a, b)));

    [DebuggerStepThrough]
    public static async Task<Writer<GenericResult<C, TError>, L>> SelectMany<A, B, C, TError, L>(
        this Task<Writer<GenericResult<A, TError>, L>> writer,
        Func<A, GenericResult<B, TError>> f,
        Func<A, B, C> resultSelector) =>
        (await writer).SelectMany(f, resultSelector);

    [DebuggerStepThrough]
    public static Task<Writer<GenericResult<C, TError>, L>> SelectMany<A, B, C, TError, L>(
        this Writer<GenericResult<A, TError>, L> writer,
        Func<A, Task<GenericResult<B, TError>>> f,
        Func<A, B, C> resultSelector) =>
        writer.Bind(async a => (await f(a)).Map(b => resultSelector(a, b)));

    // async select writer
    [DebuggerStepThrough]
    public static Task<Writer<GenericResult<C, TError>, L>> SelectMany<A, B, C, TError, L>(
        this Task<Writer<GenericResult<A, TError>, L>> writer,
        Func<A, Task<Writer<GenericResult<B, TError>, L>>> f,
        Func<A, B, C> resultSelector) =>
        writer.Bind(async a => (await f(a)).Map(br => br.Map(b => resultSelector(a, b))));

    [DebuggerStepThrough]
    public static Task<Writer<GenericResult<C, TError>, L>> SelectMany<A, B, C, TError, L>(
        this Task<Writer<GenericResult<A, TError>, L>> writer,
        Func<A, Writer<GenericResult<B, TError>, L>> f,
        Func<A, B, C> resultSelector) =>
        writer.Bind(a => f(a).Map(br => br.Map(b => resultSelector(a, b))));

    [DebuggerStepThrough]
    public static Task<Writer<GenericResult<C, TError>, L>> SelectMany<A, B, C, TError, L>(
        this Writer<GenericResult<A, TError>, L> writer,
        Func<A, Task<Writer<GenericResult<B, TError>, L>>> f,
        Func<A, B, C> resultSelector) =>
        writer.Bind(async a => (await f(a)).Map(br => br.Map(b => resultSelector(a, b))));
}