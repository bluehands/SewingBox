using System.Collections.Immutable;

namespace ObservableExplorations.Infrastructure;

public class EventStore
{
	readonly List<Event> _events = new();
	readonly SemaphoreSlim _lock = new(1);

	public long MaxEventNumber => _events[^1].Version;

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

	public Task Add(IReadOnlyCollection<string> eventPayloads) =>
		_lock.ExecuteGuarded(() =>
			{
				var eventsCount = _events.Count + 1;
				_events.AddRange(eventPayloads.Select((e, i) => new Event(eventsCount + i, e)));
			}
		);

	public record Event(long Version, string Payload);
}