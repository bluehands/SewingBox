namespace EventSourcing.Events;

public readonly record struct StreamId(string StreamType, string Id);

public abstract record Event(long Version, DateTimeOffset Timestamp, bool IsFirstOfStreamHint, EventPayload Payload)
{
	public StreamId StreamId => Payload.StreamId;

	public string Type => Payload.EventType;

	public override string ToString() =>
		$"{nameof(StreamId)}: {StreamId}, {nameof(Type)}: {Type}, {nameof(Version)}: {Version}, {nameof(Timestamp)}: {Timestamp}, {nameof(Payload)}: {Payload}";
}

public sealed record Event<T>(long Version, DateTimeOffset Timestamp, bool IsFirstOfStreamHint, T Payload)
	: Event(Version, Timestamp, IsFirstOfStreamHint, Payload) where T : EventPayload
{
	public new T Payload => (T)base.Payload;
}