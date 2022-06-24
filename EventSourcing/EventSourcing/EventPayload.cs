namespace EventSourcing;

public abstract partial record EventPayload(string StreamId, string EventType);