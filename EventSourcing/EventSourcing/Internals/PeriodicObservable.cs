using System.Reactive.Concurrency;
using System.Reactive.Linq;
using FunicularSwitch.Generators;
using Microsoft.Extensions.Logging;

namespace EventSourcing.Internals;

public static class PeriodicObservable
{
    public abstract class PollStrategy<TSource, TArg>
    {
        readonly Func<TSource, TArg> _getEventNr;

        protected PollStrategy(Func<TSource, TArg> getEventNr) => _getEventNr = getEventNr;

        public abstract Task<TArg> GetPollPosition(ReadResult<IReadOnlyList<TSource>> lastResult, TArg lastPolledPosition);

        protected TArg GetNextPosition(IReadOnlyList<TSource> lastResult, TArg lastPolledPosition) =>
            lastResult.Count > 0 ? _getEventNr(lastResult[lastResult.Count - 1]) : lastPolledPosition;
    }

    public class RetryForeverPollStrategy<TSource, TArg> : PollStrategy<TSource, TArg>
    {
        public RetryForeverPollStrategy(Func<TSource, TArg> getEventNr) : base(getEventNr)
        {
        }

        public override Task<TArg> GetPollPosition(ReadResult<IReadOnlyList<TSource>> lastResult,
            TArg lastPolledPosition) => Task.FromResult(GetNextPosition(lastResult.GetValueOrDefault() ?? new List<TSource>(), lastPolledPosition));
    }

    public class RetryNTimesWithExponentialBackOffPollStrategy<TSource, TArg> : PollStrategy<TSource, TArg>
    {
        readonly int _retryCount;
        readonly Func<TArg, TArg> _nextArgOnRetriesExceeded;
        readonly ILogger? _logger;
        int _retries;


        public RetryNTimesWithExponentialBackOffPollStrategy(Func<TSource, TArg> getEventNr,
            int retryCount,
            Func<TArg, TArg> nextArgOnRetriesExceeded,
            ILogger? logger) : base(getEventNr)
        {
            _retryCount = retryCount;
            _nextArgOnRetriesExceeded = nextArgOnRetriesExceeded;
            _logger = logger;
        }

        public override Task<TArg> GetPollPosition(ReadResult<IReadOnlyList<TSource>> lastResult, TArg lastPolledPosition) =>
            lastResult
                .Match(
                    ok => Task.FromResult(GetNextPosition(ok, lastPolledPosition)),
                    error => error.Match(
                        temporary: async t =>
                        {
                            _logger?.LogError(t.Exception, $"Failed to read events with poll argument {lastPolledPosition}. Database might be unavailable. Retrying in one second...");
                            await Task.Delay(1000).ConfigureAwait(false);
                            return lastPolledPosition;
                        },
                        permanent: async p =>
                        {
                            if (_retries >= _retryCount)
                            {
                                _retries = 0;
                                var nextArg = _nextArgOnRetriesExceeded(lastPolledPosition);
                                var errorMessage = $"Failed to read event at position {nextArg}. Continue to read stream at next position";
                                _logger?.LogError(p.Exception, errorMessage);
                                return nextArg;
                            }

                            _retries++;
                            var waitTime = TimeSpan.FromMilliseconds(Math.Min(Math.Pow(_retries, 2) * 1000, 5000));
                            _logger?.LogWarning($"Failed to read events with poll argument {lastPolledPosition}, Retrying in {waitTime} ({_retries} / {_retryCount})");
                            await Task.Delay(waitTime).ConfigureAwait(false);
                            return lastPolledPosition;
                        }
                    )
                );
    }

    public static IObservable<TResult> Poll<TResult, TArg>(
        Func<Task<TArg>> initialState,
        Func<TArg, Task<ReadResult<TResult>>> poll,
        Func<ReadResult<TResult>, TArg, Task<TArg>> deriveNextState,
        WakeUp wakeUp,
        IScheduler scheduler,
        ILogger? logger) =>
        Observable.Create<TResult>(observer => scheduler.ScheduleAsync(async (_, ct) =>
        {
            var isFirst = true;
            var result = default(ReadResult<TResult>);
            var arg = default(TArg);

            while (!ct.IsCancellationRequested)
            {
                var streamIsHot = false;
                try
                {
                    wakeUp.WorkIsScheduled();
                    var nextState = isFirst ? await initialState().ConfigureAwait(false) : await deriveNextState(result!, arg!).ConfigureAwait(false);
                    streamIsHot = !Equals(nextState, arg);
                    arg = nextState;
                    result = await poll(arg).ConfigureAwait(false);
                    isFirst = false;
                    if (result is ReadResult<TResult>.Ok_ ok) observer.OnNext(ok.Value);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, $"Poll failed. No events will be published on stream of type {typeof(TResult).Name}. This should not happen, make sure to wrap all exceptions to ReadResult in poll function");
                }
                try
                {
                    await wakeUp.WaitForSignalOrUntilTimeout(streamIsHot, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }));
}