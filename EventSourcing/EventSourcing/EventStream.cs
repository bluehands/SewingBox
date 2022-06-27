using System.Collections.Immutable;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using EventSourcing.Internals;
using Microsoft.Extensions.Logging;

namespace EventSourcing;

public sealed class EventStream<T> : IDisposable, IObservable<T>
{
	readonly IConnectableObservable<T> _stream;
	IDisposable? _connection;

	public EventStream(IObservable<T> events) => _stream = events.Publish();

	public void Start()
	{
		Stop();
		_connection = _stream.Connect();
	}

	public void Stop()
	{
		_connection?.Dispose();
		_connection = null;
	}

	public void Dispose()
	{
		Stop();
	}

	public IDisposable Subscribe(IObserver<T> observer)
		=> _stream.Subscribe(observer);
}

public static class EventStream
{
	public static EventStream<T> CreateWithPolling<TSource, T>(Func<Task<long>> getLastProcessedEventNr,
		Func<TSource, long> getEventNr,
		Func<long, Task<IReadOnlyList<TSource>>> getOrderedNewEvents,
		TimeSpan pollInterval,
		Func<IEnumerable<TSource>, Task<IEnumerable<T>>> getEvents,
		ILogger? logger)
	{
		var eventNrStream = PeriodicObservable.Poll(
			getLastProcessedEventNr,
			getOrderedNewEvents,
			(lastResult, lastArg) => lastResult.Count > 0 ? getEventNr(lastResult[lastResult.Count - 1]) : lastArg, pollInterval,
			Scheduler.Default,
			logger);

		var stream = eventNrStream
			.SelectManyPreserveOrder(async sourceEvents =>
			{
				if (sourceEvents.Count <= 0) { return Enumerable.Empty<T>(); }

				var events = (await getEvents(sourceEvents).ConfigureAwait(false)).ToImmutableList();
				if (events.Count < sourceEvents.Count)
					logger?.LogError($"Events missing. Loaded only {events.Count} of {sourceEvents.Count} events for source events: {string.Join(",", sourceEvents.Select(getEventNr))}");

				logger?.LogInformation($"Publishing event range: {getEventNr(sourceEvents[0])} - {getEventNr(sourceEvents[sourceEvents.Count - 1])}");
				return events;
			}, logger , 1)
			.SelectMany(_ => _);

		return Create(stream);
	}

	static EventStream<T> Create<T>(IObservable<T> events) => new(events);
}