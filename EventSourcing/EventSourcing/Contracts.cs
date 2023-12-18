using EventSourcing.Commands;
using EventSourcing.Events;
using FunicularSwitch;
using FunicularSwitch.Generators;

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
	Task<IEnumerable<Event>> ReadEvents(StreamId streamId, long upToPositionExclusive);
	Task<ReadResult<IReadOnlyList<Event>>> ReadEvents(long fromPositionInclusive);
}

public delegate Task<IEnumerable<Event>> ReadEventsByStreamId(StreamId streamId, long upToVersionExclusive);

public delegate Task<ReadResult<IReadOnlyList<Event>>> ReadEvents(long fromPositionInclusive = 0);

public delegate Task ExecuteCommand(Command command);
public delegate Task<OperationResult<Unit>> ExecuteCommandAndWaitUntilApplied(Command command, IObservable<CommandProcessed> processedStream);

[ResultType(typeof(ReadFailure))]
public abstract partial class ReadResult
{
}

[UnionType(CaseOrder = CaseOrder.AsDeclared)]
public abstract class ReadFailure
{
    public static ReadFailure Temporary(Exception exception) => new Temporary_(exception);
    public static ReadFailure Permanent(Exception exception) => new Permanent_(exception);

    public Exception Exception { get; }

    public class Temporary_ : ReadFailure
    {
        public Temporary_(Exception exception) : base(UnionCases.Temporary, exception)
        {
        }
    }

    public class Permanent_ : ReadFailure
    {
        public Permanent_(Exception exception) : base(UnionCases.Permanent, exception)
        {
        }
    }

    internal enum UnionCases
    {
        Temporary,
        Permanent
    }

    internal UnionCases UnionCase { get; }

    ReadFailure(UnionCases unionCase, Exception exception)
    {
        UnionCase = unionCase;
        Exception = exception;
    }

    public override string ToString() => Enum.GetName(typeof(UnionCases), UnionCase) ?? UnionCase.ToString();
    bool Equals(ReadFailure other) => UnionCase == other.UnionCase;
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj))
            return false;
        if (ReferenceEquals(this, obj))
            return true;
        if (obj.GetType() != GetType())
            return false;
        return Equals((ReadFailure)obj);
    }

    public override int GetHashCode() => (int)UnionCase;
}