using FunicularSwitch;
using FunicularSwitch.Generators;
using Microsoft.Extensions.Logging;

namespace EventSourcing.Example.Domain;

[UnionType]
public abstract record AccountPayload(string AccountId, string EventType) : EventPayload(StreamIds.Account(AccountId), EventType);

public record AccountCreated(string AccountId, string Owner, decimal InitialBalance) : AccountPayload(AccountId, EventTypes.AccountCreated);

public record PaymentReceived(string AccountId, decimal Amount) : AccountPayload(AccountId, EventTypes.PaymentReceived);
public record PaymentMade(string AccountId, decimal Amount) : AccountPayload(AccountId, EventTypes.PaymentMade);

public static class StreamTypes
{
	public const string Account = "Account";
}

public static class StreamIds
{
	public static string Account(string accountId) => $"{StreamTypes.Account}_{accountId}";

	public static bool IsAccount(string streamId) => streamId.StartsWith(StreamTypes.Account);
}

public static class EventTypes
{
	public const string AccountCreated = "AccountCreated";
	public const string PaymentReceived = "PaymentReceived";
	public const string PaymentMade = "PaymentMade";
}

public class Accounts : ProjectionCache<Account>
{
	readonly ILogger<Accounts> _logger;

	public Accounts(IObservable<Event> events, LoadEventsByStreamId loadEventsByStreamId, LoadAllEvents loadAllEvents, ILogger<Accounts> logger) 
		: base(NoEvictionCacheCollection<string, Account>.Empty, events, loadEventsByStreamId, loadAllEvents, e => StreamIds.IsAccount(e.StreamId), logger) =>
		_logger = logger;

	protected override Option<Account> InternalApply(Option<Account> account, Event @event) => @event.Payload switch
	{
		AccountPayload accountPayload => accountPayload.Match(
			accountCreated: accountCreated => new Account(accountCreated.AccountId, accountCreated.Owner, accountCreated.InitialBalance),
			paymentReceived: paymentReceived => ApplyIfExists(account, paymentReceived, a => a with { Balance = a.Balance + paymentReceived.Amount }),
			paymentMade: paymentMade => ApplyIfExists(account, paymentMade, a => a with { Balance = a.Balance - paymentMade.Amount })
			),
		_ => account
	};

	Option<Account> ApplyIfExists(Option<Account> account, AccountPayload payload, Func<Account, Option<Account>> apply)
	{
		return account.Match(
			apply,
			() =>
			{
				_logger.LogWarning($"Event {payload} for non existing account {payload.AccountId}. Ignored");
				return account;
			});
	}
}

public record Account(string Id, string Owner, decimal Balance);
