using EventSourcing;
using EventSourcing.Events;
using EventSourcing.Projections;
using Example.Domain.Events;
using FunicularSwitch;
using Microsoft.Extensions.Logging;

namespace Example.Domain.Projections;

public record Account(string Id, string Owner, decimal Balance);

public class Accounts : ProjectionCache<Account>
{
	readonly ILogger<Accounts> _logger;

	public Accounts(IObservable<Event> events, LoadEventsByStreamId loadEventsByStreamId, LoadAllEvents loadAllEvents,
		ILogger<Accounts> logger)
		: base(NoEvictionCacheCollection<StreamId, Account>.Empty, events, loadEventsByStreamId, loadAllEvents,
			e => e.StreamId.StreamType == StreamTypes.Account, logger) =>
		_logger = logger;

	protected override Option<Account> InternalApply(Option<Account> account, Event @event) => @event.Payload switch
	{
		AccountPayload accountPayload => accountPayload.Match(
			accountCreated: accountCreated =>
				new Account(accountCreated.AccountId, accountCreated.Owner, accountCreated.InitialBalance),
			paymentReceived: paymentReceived => 
				ApplyIfExists(account, paymentReceived, a => a with { Balance = a.Balance + paymentReceived.Amount }),
			paymentMade: paymentMade =>
				ApplyIfExists(account, paymentMade, a => a with { Balance = a.Balance - paymentMade.Amount })
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