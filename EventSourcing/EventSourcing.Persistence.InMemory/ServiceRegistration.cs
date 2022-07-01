using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Events;
using EventSourcing.Internals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventSourcing.Persistence.InMemory;

public class InMemoryEventStore : IEventReader, IEventWriter
{
	readonly List<Event> _events = new();
	readonly SemaphoreSlim _lock = new(1);

	public long MaxEventNumber => _events[_events.Count - 1].Position;

	public Task<IReadOnlyList<Event>> ReadEvents(long fromPositionInclusive) =>
		_lock.ExecuteGuarded(() =>
			{
				var versionExclusive = fromPositionInclusive + 1;
				if (_events.Count <= versionExclusive)
					return (IReadOnlyList<Event>)ImmutableList<Event>.Empty;
				var readOnlyList = _events.GetRange((int)versionExclusive, (int)(_events.Count - versionExclusive))
					.ToImmutableList();
				return readOnlyList;
			}
		);

	public Task<IEnumerable<Event>> ReadEvents(StreamId streamId, long upToPositionExclusive) => _lock.ExecuteGuarded(() => (IEnumerable<Event>)_events.Where(e => e.StreamId == streamId && e.Position < upToPositionExclusive).ToImmutableArray());

	public Task WriteEvents(IReadOnlyCollection<EventPayload> eventPayloads) =>
		_lock.ExecuteGuarded(() =>
			{
				var eventsCount = _events.Count + 1;
				_events.AddRange(eventPayloads.Select((e, i) => EventFactory.EventFromPayload(e, eventsCount + i, DateTime.UtcNow, false)));
			}
		);
}

public record InMemoryEventStoryOptions(TimeSpan PollInterval, PeriodicObservable.PollStrategy<Event, long> PollStrategy);


public static class ServiceRegistration
{
	public static IServiceCollection AddInMemoryEventStore(this IServiceCollection services, InMemoryEventStoryOptions? options = null)
	{
		services.AddSingleton<InMemoryEventStore>();

		services.AddEventSourcing(new EventStoreServices(), options ?? new InMemoryEventStoryOptions(TimeSpan.FromMilliseconds(100), new PeriodicObservable.RetryForeverPollStrategy<Event, long>(e => e.Position)));

		return services;
	}

	class EventStoreServices : IEventStoreServiceRegistration<InMemoryEventStoryOptions>
	{
		public EventStream<Event> BuildEventStream(IServiceProvider provider, InMemoryEventStoryOptions options)
		{
			var inMemoryEventStore = provider.GetRequiredService<InMemoryEventStore>();
			return EventStream.CreateWithPolling(
				getLastProcessedEventNr: () => Task.FromResult(-1L),
				getEventNr: e => e.Position,
				getOrderedNewEvents: fromPositionExclusive => inMemoryEventStore.ReadEvents(fromPositionExclusive + 1),
				pollInterval: options.PollInterval,
				getEvents: Task.FromResult,
				provider.GetRequiredService<ILogger<Event>>(),
				options.PollStrategy
			);
		}

		public void AddEventReader(IServiceCollection services, InMemoryEventStoryOptions options)
		{
			services.AddTransient<IEventReader>(provider => provider.GetRequiredService<InMemoryEventStore>());
		}

		public void AddEventWriter(IServiceCollection services, InMemoryEventStoryOptions options)
		{
			services.AddTransient<IEventWriter>(provider => provider.GetRequiredService<InMemoryEventStore>());
		}

		public void AddEventSerializer(IServiceCollection services, InMemoryEventStoryOptions options)
		{
		}
	}
}