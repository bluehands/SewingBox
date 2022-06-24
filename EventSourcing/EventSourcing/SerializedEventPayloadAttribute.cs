namespace EventSourcing;

[AttributeUsage(AttributeTargets.Class)]
public sealed class SerializedEventPayloadAttribute : Attribute
{
	public string EventType { get; }

	public SerializedEventPayloadAttribute(string eventType) => EventType = eventType;
}