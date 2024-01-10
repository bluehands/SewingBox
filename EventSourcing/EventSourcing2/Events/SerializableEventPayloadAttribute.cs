using System;

namespace EventSourcing2.Events;

[AttributeUsage(AttributeTargets.Class)]
public sealed class SerializableEventPayloadAttribute(string eventType) : Attribute
{
	public string EventType { get; } = eventType;
}