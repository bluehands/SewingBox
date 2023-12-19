using System;
using System.Collections.Generic;
using EventSourcing2.Events;

namespace EventSourcing2
{
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

    public interface IEventSerializer<TSerialized>
    {
        public TSerialized Serialize(object serializablePayload);
        public object Deserialize(Type serializablePayloadType, TSerialized serializedPayload);
    }

    public class EventReader<TDbEvent, TSerializedPayload>
    {
        //TODO: register payload mappers at di and receive them in ctor, then move deserialize call to read method.

        readonly IEventReader<TDbEvent> _eventReader;
        readonly IEventSerializer<TSerializedPayload>? _payloadSerializer;
        readonly IDbEventDescriptor<TDbEvent, TSerializedPayload> _eventDescriptor;

        public EventReader(
            IEventReader<TDbEvent> eventReader, 
            IDbEventDescriptor<TDbEvent, TSerializedPayload> eventDescriptor, 
            IEventSerializer<TSerializedPayload>? payloadSerializer = null)
        {
            _eventReader = eventReader;
            _payloadSerializer = payloadSerializer;
            _eventDescriptor = eventDescriptor;
        }

        public IAsyncEnumerable<Event> ReadEvents(long fromPositionInclusive)
        {
            var dbEvents = _eventReader.ReadEvents(fromPositionInclusive);
            return MapFromDbEvents(dbEvents);
        }

        async IAsyncEnumerable<Event> MapFromDbEvents(IAsyncEnumerable<TDbEvent> dbEvents)
        {
            await foreach (var dbEvent in dbEvents)
            {
                EventPayload eventPayload;
                try
                {
                    var eventType = _eventDescriptor.GetEventType(dbEvent);
                    var serializedPayload = _eventDescriptor.GetPayload(dbEvent)!;

                    eventPayload = EventPayloadMapper.MapFromSerializedPayload(eventType, serializedPayload, _payloadSerializer != null
                        ? (t, s) => _payloadSerializer.Deserialize(t, (TSerializedPayload)s)
                        : null);
                }
                catch (Exception e)
                {
                    // permanent deserialize / mapping error. 
                    // log and handle with policy, perhaps allow creating an 'UnreadableEvent' payload
                    continue;
                }

                var eventPosition = _eventDescriptor.GetPosition(dbEvent);
                var timestamp = _eventDescriptor.GetTimestamp();

                yield return EventFactory.EventFromPayload(eventPayload, eventPosition, timestamp);
            }
        }
    }

    public interface IDbEventDescriptor<in TDbEvent, out TSerializedPayload>
    {
        string GetEventType(TDbEvent dbEvent);
        TSerializedPayload GetPayload(TDbEvent dbEvent);
        long GetPosition(TDbEvent dbEvent);
        DateTimeOffset GetTimestamp();
    }
}