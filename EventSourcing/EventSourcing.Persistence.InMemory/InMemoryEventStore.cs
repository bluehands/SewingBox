using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventSourcing.Events;
using EventSourcing.Internals;

namespace EventSourcing.Persistence.InMemory;

public class InMemoryEventStore : IEventReader, IEventWriter
{
	readonly List<Event> _events = new();
	readonly SemaphoreSlim _lock = new(1);

	public long MaxEventNumber => _events[_events.Count - 1].Position;

	public Task<ReadResult<IReadOnlyList<Event>>> ReadEvents(long fromPositionInclusive) =>
		_lock.ExecuteGuarded(() =>
			{
				var versionExclusive = fromPositionInclusive - 1;
				if (versionExclusive < 0)
					versionExclusive = 0;

                IReadOnlyList<Event> result;
				if (_events.Count <= versionExclusive)
					result = ImmutableList<Event>.Empty;
                else
                {
                    result = _events.GetRange((int)versionExclusive, (int)(_events.Count - versionExclusive))
                        .ToImmutableList();
                }
                
                return ReadResult.Ok(result);
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