using System.Collections.Immutable;
using System.Reactive.Linq;

namespace EventSourcing.Events;

public class EventStreamFactory<T>
{
	readonly Func<long, Task<IEnumerable<T>>> _readEvents;
	readonly IObservable<T> _eventStream;
	readonly Func<T, long> _getPosition;

	public EventStreamFactory(Func<long, Task<IEnumerable<T>>> readEvents, IObservable<T> eventStream, Func<T, long> getPosition)
	{
		_readEvents = readEvents;
		_eventStream = eventStream;
		_getPosition = getPosition;
	}

	public IObservable<T> GetEventStream(long? fromPositionInclusive)
    {
        if (fromPositionInclusive == null)
            return _eventStream;
        
        return Observable.Create<T>(async (observer, _) =>
        {
            var isLoading = true;
            var syncObjc = new object();

            var eventsReceivedOnLoad = new List<T>();
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
            }, onCompleted: observer.OnCompleted);

            var allEvents = await _readEvents(fromPositionInclusive.Value);

            long maxEventPosition = -1;
            foreach (var @event in allEvents)
            {
                observer.OnNext(@event);
                maxEventPosition = _getPosition(@event);
            }

            lock (syncObjc)
            {
                isLoading = false;
                foreach (var @event in eventsReceivedOnLoad.Where(e => _getPosition(e) > maxEventPosition))
                    observer.OnNext(@event);
            }

            return onLoadSubscription;
        });
    }
}

public class EventStreamFactory : EventStreamFactory<Event>
{
	public EventStreamFactory(ReadEvents readEvents, IObservable<Event> eventStream) : base(async fromVersionInclusive => (await readEvents(fromVersionInclusive)).GetValueOrDefault(() => ImmutableList<Event>.Empty), eventStream, e => e.Position)
	{
	}
}