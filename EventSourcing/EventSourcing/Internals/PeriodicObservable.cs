using System.Reactive.Concurrency;
using System.Reactive.Linq;
using Microsoft.Extensions.Logging;

namespace EventSourcing.Internals;

public static class PeriodicObservable
{
	public abstract class PollStrategy<TSource, TArg>
	{
		readonly Func<TSource, TArg> _getEventNr;

		protected PollStrategy(Func<TSource, TArg> getEventNr) => _getEventNr = getEventNr;

		public abstract TArg GetPollPosition(IReadOnlyList<TSource> lastResult, bool lastPollFailed, TArg lastPolledPosition);

		protected TArg GetNextPosition(IReadOnlyList<TSource> lastResult, TArg lastPolledPosition) => lastResult.Count > 0 ? _getEventNr(lastResult[lastResult.Count - 1]) : lastPolledPosition;
	}

	public class RetryForeverPollStrategy<TSource, TArg> : PollStrategy<TSource, TArg>
	{
		public RetryForeverPollStrategy(Func<TSource, TArg> getEventNr) : base(getEventNr)
		{
		}

		public override TArg GetPollPosition(IReadOnlyList<TSource> lastResult, bool lastPollFailed, TArg lastPolledPosition) => GetNextPosition(lastResult, lastPolledPosition);
	}

	public class RetryNTimesPollStrategy<TSource, TArg> : PollStrategy<TSource, TArg>
	{
		readonly int _retryCount;
		readonly Func<TArg, TArg> _nextArgOnRetriesExceeded;
		int _retries;


		public RetryNTimesPollStrategy(Func<TSource, TArg> getEventNr, int retryCount, Func<TArg, TArg> nextArgOnRetriesExceeded) : base(getEventNr)
		{
			_retryCount = retryCount;
			_nextArgOnRetriesExceeded = nextArgOnRetriesExceeded;
		}

		public override TArg GetPollPosition(IReadOnlyList<TSource> lastResult, bool lastPollFailed, TArg lastPolledPosition) {
			if (lastPollFailed)
			{
				if (_retries >= _retryCount)
				{
					_retries = 0;
					return _nextArgOnRetriesExceeded(lastPolledPosition);
				}

				_retries++;
				return lastPolledPosition;
			}

			_retries = 0;
			return GetNextPosition(lastResult, lastPolledPosition);
		}
	}

	public static IObservable<TResult> Poll<TResult, TArg>(
		Func<Task<TArg>> initialState,
		Func<TArg, Task<TResult>> poll,
		Func<TResult, bool, TArg, TArg> deriveNextState,
		WakeUp wakeUp,
		IScheduler scheduler,
		ILogger? logger) =>
		Observable.Create<TResult>(observer => scheduler.ScheduleAsync(async (_, ct) =>
		{
			var isFirst = true;
			var result = default(TResult);
			var arg = default(TArg);
			var lastPollFailed = false;

			while (!ct.IsCancellationRequested)
			{
				var streamIsHot = false;
				try
				{
					wakeUp.WorkIsScheduled();
					var nextState = isFirst ? await initialState().ConfigureAwait(false) : deriveNextState(result!, lastPollFailed, arg!);
					streamIsHot = !Equals(nextState, arg);
					arg = nextState;
					result = await poll(arg).ConfigureAwait(false);
					isFirst = false;
					observer.OnNext(result);
					lastPollFailed = false;
				}
				catch (Exception ex)
				{
					logger?.LogError(ex, $"Poll failed. No events will be published on stream of type {typeof(TResult).Name}");
					lastPollFailed = true;
				}
				try
				{
					await wakeUp.WaitForSignalOrUntilTimeout(streamIsHot, ct);
					//await ctrl.Sleep(interval, ct).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					break;
				}
			}
		}));
}