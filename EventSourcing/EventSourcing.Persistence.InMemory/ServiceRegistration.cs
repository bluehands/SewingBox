using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Internals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventSourcing.Persistence.InMemory;

public class InMemoryEventStore
{
	readonly List<Event> _events = new();
	readonly SemaphoreSlim _lock = new(1);

	public long MaxEventNumber => _events[_events.Count - 1].Version;

	public Task<IReadOnlyList<Event>> GetAllEvents() => _lock.ExecuteGuarded(() => (IReadOnlyList<Event>)_events.ToImmutableList());

	public Task<IReadOnlyList<Event>> GetNewerEvents(long versionExclusive) =>
		_lock.ExecuteGuarded(() =>
			{
				if (_events.Count <= versionExclusive)
					return (IReadOnlyList<Event>)ImmutableList<Event>.Empty;
				var readOnlyList = _events.GetRange((int)versionExclusive, (int)(_events.Count - versionExclusive))
					.ToImmutableList();
				return readOnlyList;
			}
		);

	public Task Add(IReadOnlyCollection<EventPayload> eventPayloads) =>
		_lock.ExecuteGuarded(() =>
			{
				var eventsCount = _events.Count + 1;
				_events.AddRange(eventPayloads.Select((e, i) => EventFactory.EventFromPayload(e, eventsCount + i, DateTime.UtcNow, false)));
			}
		);

	public Task<IEnumerable<Event>> GetEventUpToVersion(string streamId, long upToVersionExclusive) => _lock.ExecuteGuarded(() => (IEnumerable<Event>)_events.Where(e => e.StreamId == streamId && e.Version < upToVersionExclusive).ToImmutableArray());
}

public static class ServiceRegistration
{
	public static void AddInMemoryEventStore(this IServiceCollection services)
	{
		services.AddSingleton<InMemoryEventStore>();

		services.AddSingleton(provider =>
			{
				var inMemoryEventStore = provider.GetRequiredService<InMemoryEventStore>();
				return EventStream.CreateWithPolling(
					getLastProcessedEventNr: () => Task.FromResult(0L),
					getEventNr: e => e.Version,
					getOrderedNewEvents: versionExclusive => inMemoryEventStore.GetNewerEvents(versionExclusive),
					pollInterval: TimeSpan.FromMilliseconds(100),
					getEvents: Task.FromResult,
					provider.GetService<ILogger<Event>>());
			});

		services.AddSingleton<IObservable<Event>>(provider => provider.GetRequiredService<EventStream<Event>>());


		services.AddSingleton<WriteEvents>(provider =>
			payloads => provider.GetRequiredService<InMemoryEventStore>().Add(payloads));

		services.AddSingleton<LoadAllEvents>(serviceProvider =>
			async () => await serviceProvider.GetRequiredService<InMemoryEventStore>().GetAllEvents());

		services.AddSingleton<LoadEventsByStreamId>(serviceProvider => 
			(streamId, upToVersionExclusive) => serviceProvider.GetRequiredService<InMemoryEventStore>().GetEventUpToVersion(streamId, upToVersionExclusive));

	}
}