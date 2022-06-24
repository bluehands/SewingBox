namespace EventSourcing;

public delegate Task WriteEvents(IReadOnlyCollection<EventPayload> payloads);

public delegate Task<IEnumerable<Event>> LoadEventsByStreamId(string streamId, long upToVersionExclusive);

public delegate Task<IEnumerable<Event>> LoadAllEvents();