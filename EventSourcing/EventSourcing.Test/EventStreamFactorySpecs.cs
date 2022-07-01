using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using EventSourcing.Events;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EventSourcing.Test;

[TestClass]
public class EventFactorySpecs
{
	record MyEvent(long Position);

	[TestMethod]
	public async Task WhenSubscribingWithEventsPublishedOnBootstrap()
	{
		static Task<IEnumerable<MyEvent>> ReadEvents(long fromPositionInclusive) =>
			Task.FromResult(
				new[]
					{
						0, 1, 2, 3, 4
					}
					.Select(position => new MyEvent(position))
			);

		var eventStream = Observable.Create<MyEvent>(async observer =>
		{
			//Position 4 is published on live stream and is read from store
			observer.OnNext(new (4));
			observer.OnNext(new (5));
			await Task.Delay(200);
			observer.OnNext(new (6));
			observer.OnCompleted();
		});

		var eventStreamFactory = new EventStreamFactory<MyEvent>(ReadEvents, eventStream, e => e.Position);
		
		var allEvents = eventStreamFactory.GetEventStream(0);
		var eventsStartingAt3 = eventStreamFactory.GetEventStream(3);

		var eventsStartingAt3List = await eventsStartingAt3.ToList().ToTask();
		eventsStartingAt3List.Select(e => e.Position).Should().ContainInOrder(3, 4, 5, 6);

		var allEventsList = await allEvents.ToList().ToTask();
		allEventsList.Select(e => e.Position).Should().ContainInOrder(0, 1, 2, 3, 4, 5, 6);
	}
}