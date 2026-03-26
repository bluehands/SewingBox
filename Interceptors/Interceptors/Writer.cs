using System.Collections.Immutable;
using FunicularSwitch;

namespace Interceptors;

public static class Writer
{
    public static Writer<T, E> Empty<T, E>(T value) => Writer<T, E>.Write(value, ImmutableList<E>.Empty);
    public static Writer<Unit, E> Unit<E>() => Writer<Unit, E>.Write(No.Thing, ImmutableList<E>.Empty);

    public static Writer<T, E> Write<T, E>(T value, E element) => Write<T, E>(value, ImmutableList.Create(element));
    public static Writer<T, E> Write<T, E>(T value, IImmutableList<E> elements) => Writer<T, E>.Write(value, elements);
}

public static class Writer<E>
{
    public static Writer<T, E> Empty<T>(T value) => Writer<T, E>.Write(value, ImmutableList<E>.Empty);
}

public readonly struct Writer<T, E>
{
    private readonly T currentValue;
    private readonly Option<IImmutableList<E>> writtenElements;

    public (T, IEnumerable<E>) Unwrap() => (currentValue, writtenElements.GetValueOrDefault(ImmutableList<E>.Empty));

    public void Deconstruct(out T value, out IImmutableList<E> elements)
    {
        value = currentValue;
        elements = writtenElements.GetValueOrDefault(ImmutableList<E>.Empty);
    }

    public static Writer<T, E> Write(T value, IImmutableList<E> elements) => new(value, elements);

    public Writer(T currentValue, IImmutableList<E> writtenElements)
    {
        this.currentValue = currentValue;
        this.writtenElements = Option<IImmutableList<E>>.Some(writtenElements);
    }

    public Writer<R, E> Map<R>(Func<T, R> fn) => Bind(v => Writer<R, E>.Write(fn(v), ImmutableList<E>.Empty));

    public async Task<Writer<R, E>> Map<R>(Func<T, Task<R>> fn) => await Bind(async v => Writer<R, E>.Write(await fn(v), ImmutableList<E>.Empty));

    public Writer<R, E> Bind<R>(Func<T, Writer<R, E>> fn)
    {
        var (v, newElements) = fn(currentValue);
        var oldElements = writtenElements.GetValueOrDefault(ImmutableList<E>.Empty);
        var combined = oldElements.AddRange(newElements);
        return new Writer<R, E>(v, combined);
    }

    public async Task<Writer<R, E>> Bind<R>(Func<T, Task<Writer<R, E>>> fn)
    {
        var (v, newElements) = await fn(currentValue);
        var oldElements = writtenElements.GetValueOrDefault(ImmutableList<E>.Empty);
        var combined = oldElements.AddRange(newElements);
        return new Writer<R, E>(v, combined);
    }
    
    public Writer<T, E> Write(E element) => Write(currentValue, ImmutableList.Create(element));
    public Writer<T, E> Write(IImmutableList<E> elements) => Write(currentValue, elements);
}