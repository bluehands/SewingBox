using EventSourcing.Events;

namespace Example.Domain.Events;

public static class StreamIds
{
	public static StreamId Account(string accountId) => new (StreamTypes.Account, accountId);
}