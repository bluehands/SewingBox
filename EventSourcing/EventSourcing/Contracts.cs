using EventSourcing.Commands;
using EventSourcing.Events;
using FunicularSwitch;

namespace EventSourcing;

public interface IEventSerializer<TSerialized>
{
	public TSerialized Serialize(object serializablePayload);
	public object Deserialize(Type serializablePayloadType, TSerialized serializedPayload);
}

public interface IEventWriter
{
	Task WriteEvents(IReadOnlyCollection<EventPayload> payloads);
}

public delegate Task WriteEvents(IReadOnlyCollection<EventPayload> payloads);

public interface IEventReader
{
	Task<IEnumerable<Event>> ReadEvents(StreamId streamId, long upToVersionExclusive);
	Task<IEnumerable<Event>> ReadEvents();
	Task<IReadOnlyList<Event>> ReadEvents(long fromPositionInclusive);
}

public delegate Task<IEnumerable<Event>> LoadEventsByStreamId(StreamId streamId, long upToVersionExclusive);

public delegate Task<IEnumerable<Event>> LoadAllEvents();

public delegate Task ExecuteCommand(Command command);
public delegate Task<OperationResult<Unit>> ExecuteCommandAndWaitUntilApplied(Command command, IObservable<CommandProcessed> processedStream);