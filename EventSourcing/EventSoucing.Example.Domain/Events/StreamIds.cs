namespace EventSourcing.Example.Domain.Events;

public static class StreamIds
{
	public static string Account(string accountId) => $"{StreamTypes.Account}_{accountId}";

	public static bool IsAccount(string streamId) => streamId.StartsWith(StreamTypes.Account);
}