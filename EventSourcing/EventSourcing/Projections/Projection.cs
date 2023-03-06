using System.Reactive.Linq;
using System.Reactive.Subjects;
using EventSourcing.Events;
using Microsoft.Extensions.Logging;

namespace EventSourcing.Projections;

public abstract class Projection<T> where T : notnull
{
	readonly IConnectableObservable<(T state, Event)> _connectableObservable;
	IDisposable? _connection;
	public IObservable<(T state, Event @event)> Changes => _connectableObservable;
	public T Current { get; private set; }

	public IObservable<CommandProcessed> ProcessedCommands => Changes
		.Select(e => e.@event.Payload)
		.OfType<CommandProcessed>();

	public IObservable<(object state, Event @event)> SeenEvents => Changes.Select(t => ((object)t.state, t.@event));

	protected Projection(IObservable<Event> eventStream, ILogger<Projection<T>> logger, T initialState, Func<T, Event, T> apply)
	{
		Current = initialState;
		_connectableObservable = eventStream
			.Scan((state: initialState, (Event)null!), (state, @event) =>
			{
				var stateBeforeApply = state.state;
				try
				{
					var updated = apply(stateBeforeApply, @event);
					Current = updated;
					logger.LogDebug("Projection {projectionType} processed event {event}", GetType().Name, @event);
					return (updated, @event);
				}
				catch (Exception e)
				{
					logger.LogError(e, "{projectionType} apply failed for event {event}. State is unchanged.", GetType().Name, @event);
					return (state: stateBeforeApply, @event);
				}
			})
			.Publish();
	}

	public virtual void Connect()
	{
		_connection = _connectableObservable.Connect();
	}

	public virtual void Dispose()
	{
		_connection?.Dispose();
	}
}