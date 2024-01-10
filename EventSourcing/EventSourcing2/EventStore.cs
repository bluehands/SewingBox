using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EventSourcing2.Events;

namespace EventSourcing2;

public interface IEventStore
{
    IAsyncEnumerable<Event> ReadEvents(long fromPositionInclusive);
    Task WriteEvents(IReadOnlyCollection<EventPayload> payloads);
}

public class EventStore<TDbEvent, TSerializedPayload> : IEventStore
{
    //TODO: register payload mappers at di and receive them in ctor, then move deserialize call to read method.

    readonly IEventReader<TDbEvent> _eventReader;
    readonly IEventSerializer<TSerializedPayload> _payloadSerializer;
    readonly IEventWriter<TDbEvent> _eventWriter;
    readonly IDbEventDescriptor<TDbEvent, TSerializedPayload> _eventDescriptor;

    public EventStore(
        IEventReader<TDbEvent> eventReader,
        IEventWriter<TDbEvent> eventWriter,
        IDbEventDescriptor<TDbEvent, TSerializedPayload> eventDescriptor,
        IEventSerializer<TSerializedPayload> payloadSerializer
        )
    {
        _eventReader = eventReader;
        _payloadSerializer = payloadSerializer;
        _eventWriter = eventWriter;
        _eventDescriptor = eventDescriptor;
    }

    public IAsyncEnumerable<Event> ReadEvents(long fromPositionInclusive)
    {
        var dbEvents = _eventReader.ReadEvents(fromPositionInclusive);
        return MapFromDbEvents(dbEvents);
    }

    public Task WriteEvents(IReadOnlyCollection<EventPayload> payloads)
    {
        var events = MapToDbEvents(payloads);
        return _eventWriter.WriteEvents(events);
    }

    IEnumerable<TDbEvent> MapToDbEvents(IEnumerable<EventPayload> payloads) =>
        payloads.Select(payload =>
        {
            var serializedPayload = _payloadSerializer.Serialize(payload);
            return _eventDescriptor.CreateDbEvent(payload.StreamId, payload.EventType, serializedPayload);
        });

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
            var timestamp = _eventDescriptor.GetTimestamp(dbEvent);

            yield return EventFactory.EventFromPayload(eventPayload, eventPosition, timestamp);
        }
    }
}