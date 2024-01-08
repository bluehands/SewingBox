using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EventSourcing2;

public readonly record struct StreamId(string StreamType, string Id);

public abstract record Event(long Position, DateTimeOffset Timestamp, EventPayload Payload)
{
    public StreamId StreamId => Payload.StreamId;

    public string Type => Payload.EventType;

    public override string ToString() =>
        $"{nameof(StreamId)}: {StreamId}, {nameof(Type)}: {Type}, {nameof(Position)}: {Position}, {nameof(Timestamp)}: {Timestamp}, {nameof(Payload)}: {Payload}";
}

public sealed record Event<T>(long Position, DateTimeOffset Timestamp, T Payload)
    : Event(Position, Timestamp, Payload) where T : EventPayload
{
    public new T Payload => (T)base.Payload;
}

public abstract record EventPayload(StreamId StreamId, string EventType);

public interface IEventReader<out TDbEvent>
{
    IAsyncEnumerable<TDbEvent> ReadEvents(StreamId streamId);
        
    IAsyncEnumerable<TDbEvent> ReadEvents(long fromPositionInclusive);
}

public interface IEventWriter<in TDbEvent>
{
    Task WriteEvents(IEnumerable<TDbEvent> payloads);
}

public interface IEventSerializer<TSerialized>
{
    public TSerialized Serialize(object serializablePayload);
    public object Deserialize(Type serializablePayloadType, TSerialized serializedPayload);
}

public interface IDbEventDescriptor<TDbEvent, TSerializedPayload>
{
    string GetEventType(TDbEvent dbEvent);
    TSerializedPayload GetPayload(TDbEvent dbEvent);
    long GetPosition(TDbEvent dbEvent);
    DateTimeOffset GetTimestamp(TDbEvent dbEvent);

    TDbEvent CreateDbEvent(StreamId streamId, string eventType, TSerializedPayload payload);
}