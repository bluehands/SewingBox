using System.Diagnostics;
using EventSourcing.Events;
using Microsoft.Extensions.Logging;

namespace EventSourcing.Internals;

public static class EventReadHelper
{
    public static async Task<ReadResult<IReadOnlyList<Event>>> ReadEvents<TDbEvent>(Func<Task<IEnumerable<TDbEvent>>> readEventsFromDb, Func<TDbEvent, Event> map, Func<Exception, ReadFailure>? classifyDbException = null, ILogger? logger = null)
    {
        var timer = Stopwatch.StartNew();
        IEnumerable<TDbEvent> dbEvents;
        try
        {
            dbEvents = await readEventsFromDb().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            return ReadResult.Error<IReadOnlyList<Event>>(ReadFailure.Temporary(e));
        }

        var readResult = Map(dbEvents, map, classifyDbException, logger);

        if (readResult is ReadResult<IReadOnlyList<Event>>.Ok_ ok)
        {
            if (ok.Value.Count > 0 && logger != null)
                logger.LogInformation($"Read {ok.Value.Count} events from store ({timer.Elapsed})");
        }

        return readResult;
    }

    static ReadResult<IReadOnlyList<Event>> Map<TSource>(IEnumerable<TSource> commits, Func<TSource, Event> map, Func<Exception, ReadFailure>? classifyDbException, ILogger? logger)
    {
        var events = new List<Event>();
        try
        {
            //it is important to add events here one by one, because if an event cannot be interpreted we return the ones that were read successfully.
            //Do not convert to LINQ!
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var commit in commits)
            {
                try
                {
                    var mapped = map(commit);
                    events.Add(mapped);
                }
                catch (Exception e) //serialization / mapping error -> permanent
                {
                    if (events.Count != 0)
                    {
                        logger?.LogWarning(e, $"Failed to read events from store. Returning {events.Count} successful events. Next event will be retried.");
                        return events;
                    }
                    return ReadResult.Error<IReadOnlyList<Event>>(ReadFailure.Permanent(e));
                }
            }
        }
        catch (Exception e) //error from db event iterator -> temporary or error from db serialization -> decide externally
        {
            if (events.Count != 0)
                logger?.LogWarning(e, $"Failed to read events from store. Returning {events.Count} successful events. Next event will be retried.");
            else
                return ReadResult.Error<IReadOnlyList<Event>>(classifyDbException?.Invoke(e) ?? ReadFailure.Temporary(e));
        }

        return events;
    }
}