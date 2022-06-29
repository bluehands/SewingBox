namespace EventSourcing.Events;

public abstract partial record EventPayload(StreamId StreamId, string EventType);