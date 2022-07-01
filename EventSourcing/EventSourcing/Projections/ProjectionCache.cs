using System.Collections.Immutable;
using System.Reactive.Linq;
using EventSourcing.Commands;
using EventSourcing.Events;
using EventSourcing.Internals;
using FunicularSwitch;
using FunicularSwitch.Extensions;
using Microsoft.Extensions.Logging;

namespace EventSourcing.Projections;

public abstract class ProjectionCache<T> : IDisposable
{
	public IObservable<(Event @event, T projection)> AppliedEventStream { get; }
	public IObservable<CommandProcessed> CommandProcessedStream { get; }
	public IObservable<OutgoingEvent> SeenEvents { get; }

	ICacheCollection<StreamId, T> Items { get; set; }

	readonly ReadEventsByStreamId _readEventsByStreamId;
	readonly ReadEvents _readEvents;
	readonly Func<Event, bool> _eventPredicate;

	bool _allEventsLoaded;
	long _maxAppliedEventPosition;

	readonly AsyncLock _lock = new();

	protected ProjectionCache(ICacheCollection<StreamId, T> items,
		IObservable<Event> events,
		ReadEventsByStreamId readEventsByStreamId,
		ReadEvents readEvents,
		Func<Event, bool> eventPredicate,
		ILogger? logger)
	{
		Items = items;
		_readEventsByStreamId = readEventsByStreamId;
		_readEvents = readEvents;
		_eventPredicate = eventPredicate;

		SeenEvents = events
			.SelectManyPreserveOrder(@event => ApplyEvent(@event), logger, 1)
			.Publish()
			.RefCount();

		AppliedEventStream = SeenEvents.OfType<EventAppliedToProjectionEvent>().Select(s => s.EventProjectionPair)
			.Publish().RefCount();
		CommandProcessedStream = SeenEvents.OfType<CommandProcessedEvent>().Select(s => s.Event.Payload).Publish()
			.RefCount();
	}

	async Task<OutgoingEvent> ApplyEvent(Event @event)
	{
		OutgoingEvent? result = null;

		if (_eventPredicate(@event))
		{
			await _lock.ExecuteGuarded(async () =>
			{
				var upToPosition = @event.Position;
				var (state, _) = @event.IsFirstOfStreamHint
					? (Option<T>.None, 0)
					: await InternalGet(@event.StreamId, upToPosition).ConfigureAwait(false);
				var updated = InternalApply(state, @event);

				var updatedItems = updated.Match(s =>
				{
					if (ReferenceEquals(updated, state))
						return Items;

					result = OutgoingEvent.EventApplied((@event, s));
					return Items.Set(@event.StreamId, s);
				}, Items);

				InternalSet(updatedItems, @event.Position);
			}).ConfigureAwait(false);
		}
		else if (@event is Event<CommandProcessed> c)
		{
			result = OutgoingEvent.CommandProcessed(c);
		}

		return result ?? OutgoingEvent.Ignored(@event);
	}

	protected abstract Option<T> InternalApply(Option<T> state, Event @event);

	void InternalSet(ICacheCollection<StreamId, T> updatedItems, long eventPosition)
	{
		Items = updatedItems;
		_maxAppliedEventPosition = eventPosition;
	}

	async Task<(Option<T> item, Option<long> maxLoadedEventPosition)> InternalGet(StreamId id, long positionExclusive)
	{
		var state = (item: Items.TryGetValue(id), position: Option<long>.None);
		if (state.item.IsNone())
		{
			var events = await _readEventsByStreamId(id, positionExclusive).ConfigureAwait(false);
			long maxEventPosition = 0;
			var item = events.Aggregate(Option<T>.None, (s, e) =>
			{
				maxEventPosition = e.Position;
				return InternalApply(s, e);
			});
			state = (item, maxEventPosition);
		}

		return state;
	}

	public async Task<Option<T>> Get(StreamId id, long upToPosition = long.MaxValue)
	{
		var state = Items.TryGetValue(id);
		if (state.IsSome())
			return state;

		return await _lock.ExecuteGuarded(async () =>
		{
			var (item, position) = await InternalGet(id, upToPosition).ConfigureAwait(false);
			if (item.IsSome() &&
				position.Match(maxLoadedPosition => maxLoadedPosition <= _maxAppliedEventPosition, false))
			{
				InternalSet(Items.Set(id, item.GetValueOrDefault()!), _maxAppliedEventPosition);
			}

			return item;
		}).ConfigureAwait(false);
	}

	public async Task<IEnumerable<T>> GetAll()
	{
		if (_allEventsLoaded)
			return Items.GetAllValues();

		return await _lock.ExecuteGuarded(async () =>
		{
			if (_allEventsLoaded)
				return Items.GetAllValues();

			var events = await _readEvents().ConfigureAwait(false);

			long maxEventPosition = 0;

			Items = Items.Clear();

			var items = events.Aggregate(Items, (s, e) =>
			{
				maxEventPosition = e.Position;
				var item = s.TryGetValue(e.StreamId);
				var updatedItem = InternalApply(item, e);
				return updatedItem.Match(_ => s.Set(e.StreamId, _), () => s);
			});

			InternalSet(items, maxEventPosition);
			_allEventsLoaded = true;
			return Items.GetAllValues();

		}).ConfigureAwait(false);
	}

	public Task Invalidate() =>
		_lock.ExecuteGuarded(() =>
		{
			Items = Items.Clear();
			_allEventsLoaded = false;
		});

	public abstract record OutgoingEvent(Event Event)
	{
		public static OutgoingEvent EventApplied((Event e, T projection) pair) =>
			new EventAppliedToProjectionEvent(pair);

		public static OutgoingEvent CommandProcessed(Event<CommandProcessed> commandProcessedEvent) =>
			new CommandProcessedEvent(commandProcessedEvent);

		public static OutgoingEvent Ignored(Event @event) => new IgnoredEvent(@event);
	}

	public record EventAppliedToProjectionEvent((Event e, T projection) EventProjectionPair) : OutgoingEvent(EventProjectionPair.e);

	public record CommandProcessedEvent(Event<CommandProcessed> Event) : OutgoingEvent(Event)
	{
		public new Event<CommandProcessed> Event => (Event<CommandProcessed>)base.Event;
	}

	public record IgnoredEvent(Event Event) : OutgoingEvent(Event);

	public class ProjectionWaitHandle
	{
		public static readonly ProjectionWaitHandle NoWait = new(Task.CompletedTask);

		public Task Task { get; }

		public ProjectionWaitHandle(ProjectionCache<T> projectionCache, CommandStream commandStream)
		: this(commandStream.SendCommandAndWaitUntilApplied(new NoOp(), projectionCache.CommandProcessedStream))
		{
		}

		ProjectionWaitHandle(Task task) => Task = task;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
			_lock.Dispose();
		}
	}

	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}
}

public static class ProjectionCacheExtension
{
	public static ProjectionCache<T>.ProjectionWaitHandle WaitUntilEventProcessed<T>(this ProjectionCache<T> projectionCache, CommandStream commandStream) => new(projectionCache, commandStream);
}

public interface ICacheCollection<in TKey, T>
{
	Option<T> TryGetValue(TKey key);
	ICacheCollection<TKey, T> Set(TKey id, T updated);
	ICacheCollection<TKey, T> Clear();
	IEnumerable<T> GetAllValues();
}

public class NoEvictionCacheCollection<TKey, T> : ICacheCollection<TKey, T> where TKey : notnull
{
	readonly ImmutableDictionary<TKey, T> _items;

	public static readonly NoEvictionCacheCollection<TKey, T> Empty = new(ImmutableDictionary<TKey, T>.Empty);

	public NoEvictionCacheCollection(ImmutableDictionary<TKey, T> items) => _items = items;

	public Option<T> TryGetValue(TKey key) => _items.TryGetValue(key);
	public ICacheCollection<TKey, T> Set(TKey id, T updated) => new NoEvictionCacheCollection<TKey, T>(_items.SetItem(id, updated));
	public ICacheCollection<TKey, T> Clear() => Empty;
	public IEnumerable<T> GetAllValues() => _items.Values;
}