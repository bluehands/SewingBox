﻿using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using EventSourcing.Events;
using EventSourcing.Internals;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace EventSourcing.Test;

public static class TestEnvironment
{
    public static IServiceProvider Services { get; }

    static TestEnvironment() =>
        Services = new ServiceCollection()
            .AddLogging(builder => builder
                .AddConsole()
                .SetMinimumLevel(LogLevel.Trace)
                )
            .BuildServiceProvider();
}


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
            observer.OnNext(new(4));
            observer.OnNext(new(5));
            await Task.Delay(200);
            observer.OnNext(new(6));
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

[TestClass]
public class PollSpecs
{
    [TestMethod]
    public async Task WithLimitedRetries_ThenUnreadableEventsAreSkipped()
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        static async Task<ReadResult<IReadOnlyList<(string e, long position)>>> GetNewEvents(long positionInclusive)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            static ReadResult<IReadOnlyList<(string e, long position)>> ToEvent(long pos) => ReadResult.Ok((IReadOnlyList<(string e, long position)>)new[] { ("ok", position: pos) });

            if (positionInclusive == 0 || positionInclusive == 2)
                return ToEvent(positionInclusive);

            return ReadResult.Error<IReadOnlyList<(string, long)>>(ReadFailure.Permanent(new Exception("Read failed")));
        }

        var stream = EventStream.CreateWithPolling(
            () => Task.FromResult(-1L),
            t => t.position,
            lastProcessedPosition => GetNewEvents(lastProcessedPosition + 1),
            new(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(1000), TestEnvironment.Services.GetRequiredService<ILogger<Event>>()),
            Task.FromResult,
            TestEnvironment.Services.GetRequiredService<ILogger<PollSpecs>>(),
            new PeriodicObservable.RetryNTimesWithExponentialBackOffPollStrategy<(string e, long position), long>(t => t.position, 1, l => l + 1, null)
        );

        var firstTwoEvents = stream.Take(2).ToList().ToTask();

        stream.Start();

        (await firstTwoEvents).Select(e => e.position).Should().ContainInOrder(0L, 2L);
    }
}