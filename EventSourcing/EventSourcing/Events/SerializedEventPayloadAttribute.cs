namespace EventSourcing.Events;

[AttributeUsage(AttributeTargets.Class)]
public sealed class SerializedEventPayloadAttribute : Attribute
{
	public string EventType { get; }

	public SerializedEventPayloadAttribute(string eventType) => EventType = eventType;
}