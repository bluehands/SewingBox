using System;

namespace EventSourcing2.Events;

[AttributeUsage(AttributeTargets.Class)]
public sealed class SerializedEventPayloadAttribute(string eventType) : Attribute
{
	public string EventType { get; } = eventType;
}