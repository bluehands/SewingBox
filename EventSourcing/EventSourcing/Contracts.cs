using FunicularSwitch;

namespace EventSourcing;

public delegate Task WriteEvents(IReadOnlyCollection<EventPayload> payloads);

public delegate Task<IEnumerable<Event>> LoadEventsByStreamId(string streamId, long upToVersionExclusive);

public delegate Task<IEnumerable<Event>> LoadAllEvents();

public delegate Task ExecuteCommand(Command command);
public delegate Task<OperationResult<Unit>> ExecuteCommandAndWaitUntilApplied(Command command, IObservable<CommandProcessed> processedStream);