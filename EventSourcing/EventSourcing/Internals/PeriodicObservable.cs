using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;

namespace EventSourcing.Internals;

class PeriodicObservable
{
	public static IObservable<TResult> Poll<TResult, TArg>(
		Func<Task<TArg>> initialState,
		Func<TArg, Task<TResult>> poll,
		Func<TResult, TArg, TArg> deriveNextState,
		TimeSpan interval,
		IScheduler scheduler,
		ILogger? logger) =>
		Observable.Create<TResult>(observer => scheduler.ScheduleAsync(async (ctrl, ct) =>
		{
			var isFirst = true;
			var result = default(TResult);
			var arg = default(TArg);

			while (!ct.IsCancellationRequested)
			{
				try
				{
					arg = isFirst ? await initialState().ConfigureAwait(false) : deriveNextState(result!, arg!);
					result = await poll(arg).ConfigureAwait(false);
					isFirst = false;
					observer.OnNext(result);
				}
				catch (Exception ex)
				{
					logger?.LogError(ex, $"Poll failed. No events will be published on stream of type {typeof(TResult).Name}");
				}
				try
				{
					await ctrl.Sleep(interval, ct).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					break;
				}
			}
		}));
}