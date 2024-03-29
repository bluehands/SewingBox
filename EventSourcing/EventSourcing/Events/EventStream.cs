﻿using System.Collections.Immutable;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using EventSourcing.Internals;
using Microsoft.Extensions.Logging;

namespace EventSourcing.Events;

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
        Func<long, Task<ReadResult<IReadOnlyList<TSource>>>> getOrderedNewEvents,
		WakeUp wakeUp,
		Func<IEnumerable<TSource>, Task<IEnumerable<T>>> getEvents,
		ILogger logger, 
		PeriodicObservable.PollStrategy<TSource, long> pollStrategy)
	{
		var eventNrStream = PeriodicObservable.Poll(
			initialState: getLastProcessedEventNr,
			poll: getOrderedNewEvents,
            deriveNextState: pollStrategy.GetPollPosition,
			wakeUp: wakeUp,
			scheduler: Scheduler.Default,
			logger: logger);

		var stream = eventNrStream
			.SelectManyPreserveOrder(async sourceEvents =>
			{
				if (sourceEvents.Count <= 0) { return Enumerable.Empty<T>(); }

				var events = (await getEvents(sourceEvents).ConfigureAwait(false)).ToImmutableList();
				if (events.Count < sourceEvents.Count)
					logger.LogError($"Events missing. Loaded only {events.Count} of {sourceEvents.Count} events for source events: {string.Join(",", sourceEvents.Select(getEventNr))}");

				logger.LogInformation($"Publishing event range: {getEventNr(sourceEvents[0])} - {getEventNr(sourceEvents[sourceEvents.Count - 1])}");
				return events;
			}, logger, 1)
			.SelectMany(e => e);

		return Create(stream);
	}

	public static EventStream<T> Create<T>(IObservable<T> events) => new(events);

	public static PeriodicObservable.PollStrategy<Event, long> PollStrategyRetryOnFail(int retryCount, ILogger? logger) => new PeriodicObservable.RetryNTimesWithExponentialBackOffPollStrategy<Event, long>(e => e.Position, retryCount, l => l + 1, logger);
	public static readonly PeriodicObservable.PollStrategy<Event, long> PollStrategyRetryForever = new PeriodicObservable.RetryForeverPollStrategy<Event, long>(e => e.Position);
}