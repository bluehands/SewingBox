using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using EventSourcing.Events;
using Example.Domain.Events;
using Example.Domain.Projections;
using JetBrains.Annotations;

namespace Example.Host.GraphQl;

[UsedImplicitly]
public class Subscription
{
	[Subscribe(With = nameof(SubscribeAccountChanges))]
	public AccountAndBalance OnAnyBalanceChanged([EventMessage] AccountAndBalance accounts) => accounts;

	public IObservable<AccountAndBalance> SubscribeAccountChanges([Service] Accounts cache) => cache.AppliedEventStream.Select(t => Map(t.projection));

	static AccountAndBalance Map(Example.Domain.Projections.Account account) => new(account.Owner, account.Balance);


	[Subscribe(With = nameof(SubscribeAccountListChanges))]
	public IReadOnlyCollection<AccountAndBalance> OnBalancesChanged([EventMessage] IReadOnlyCollection<AccountAndBalance> accounts) => accounts;

	public IObservable<IReadOnlyCollection<AccountAndBalance>> SubscribeAccountListChanges([Service] Accounts cache)
	{
		async Task<IReadOnlyCollection<AccountAndBalance>> GetAll()
		{
			var accounts = await cache.GetAll();
			return accounts
				.Select(Map)
				.OrderByDescending(o => o.Owner)
				.ToList();
		}

		return GetAll()
			.ToObservable()
			.Concat(cache.AppliedEventStream.SelectMany(_ => GetAll()));

	}

	public record AccountAndBalance(string Owner, decimal Balance);

	[Subscribe(With = nameof(SubscribeTotalBalanceChanges))]
	public decimal OnTotalBalanceChanged([EventMessage] decimal totalBalance) => totalBalance;

	public IObservable<decimal> SubscribeTotalBalanceChanges([Service] EventStreamFactory factory) => factory.GetEventStream(0).Scan(0M, (balance, @event) => @event.Payload is AccountCreated c? balance + c.InitialBalance : balance);
	
}

