using System.Reactive.Linq;

namespace EventSourcing.Events;

public class EventStreamFactory
{
	readonly IEventReader _eventReader;
	readonly IObservable<Event> _eventStream;

	public EventStreamFactory(IEventReader eventReader, IObservable<Event> eventStream)
	{
		_eventReader = eventReader;
		_eventStream = eventStream;
	}

	public IObservable<Event> GetEventStream(long fromVersionInclusive) =>
		Observable.Create<Event>(async (observer, _) =>
		{
			var isLoading = true;
			var syncObjc = new object();

			var eventsReceivedOnLoad = new List<Event>();
			var onLoadSubscription = _eventStream.Subscribe(e =>
			{
				// ReSharper disable once AccessToModifiedClosure
				if (!isLoading)
				{
					observer.OnNext(e);
					return;
				}

				lock (syncObjc)
				{
					// ReSharper disable once AccessToModifiedClosure
					if (!isLoading)
						observer.OnNext(e);
					else
						eventsReceivedOnLoad.Add(e);
				}
			});

			var allEvents = await _eventReader.ReadEvents(fromVersionInclusive);

			long maxEventVersion = -1;
			foreach (var @event in allEvents)
			{
				observer.OnNext(@event);
				maxEventVersion = @event.Version;
			}
			lock (syncObjc)
			{
				isLoading = false;
				foreach (var @event in eventsReceivedOnLoad.Where(e => e.Version > maxEventVersion))
					observer.OnNext(@event);
			}

			return onLoadSubscription;
		});
}