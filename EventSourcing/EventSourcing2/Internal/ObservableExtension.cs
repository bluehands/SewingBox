using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;

namespace EventSourcing2.Internal;

static class ObservableExtension
{
	public static IDisposable SubscribeAsync<TSource>(this IObservable<TSource> source, Func<TSource, Task> onNext, ILogger? logger, int maxParallelBatches = 1) =>
		source
			.SelectManyPreserveOrder(onNext.ToFunc(), logger, maxParallelBatches)
			.Subscribe();


	public static IObservable<TResult> SelectManyPreserveOrder<TSource, TResult>(this IObservable<TSource> source, Func<TSource, Task<TResult>> selector, ILogger? logger, int maxParallelBatches)
	{
		async Task<IEnumerable<TResult>> SafeSelector(TSource s)
		{
			try
			{
				var result = await selector(s).ConfigureAwait(false);
				return new[] { result };
			}
			catch (Exception ex)
			{
				logger?.Log(LogLevel.Error, ex, $"Error in select on source observable: {typeof(TSource)} -> {typeof(TResult)}");
				return new TResult[] { };
			}
		}

		return source.FromTplDataflow(() =>
			new TransformManyBlock<TSource, TResult>((Func<TSource, Task<IEnumerable<TResult>>>)SafeSelector,
				new() { MaxDegreeOfParallelism = maxParallelBatches }));
	}

	public static IObservable<TResult> FromTplDataflow<T, TResult>(
		this IObservable<T> source, Func<IPropagatorBlock<T, TResult>> blockFactory)
	{
		return Observable.Defer(() =>
		{
			var block = blockFactory();
			return Observable.Using(() =>
			{
				var sub = source.SelectMany(s => block.SendAsync(s)).Subscribe();
				return Disposable.Create(() => sub.Dispose());
			}, _ => block.AsObservable());
		});
	}
}

static class FuncToAction
{
	public static Action<T1> IgnoreReturn<T1, T2>(this Func<T1, T2> func) => t => func(t);

	public static Action IgnoreReturn<T>(this Func<T> func) => () => func();

	public static Func<T?> ToFunc<T>(this Action action) =>  () =>
	{
		action();
		return default;
	};

	public static Func<T, int> ToFunc<T>(this Action<T> action) => t =>
	{
		action(t);
		return 42;
	};

	public static Func<Task<T?>> ToFunc<T>(this Func<Task> action) => async () =>
	{
		await action().ConfigureAwait(false);
		return default;
	};

	public static Func<T, Task<int>> ToFunc<T>(this Func<T, Task> action) => async t =>
	{
		await action(t).ConfigureAwait(false);
		return 42;
	};
}