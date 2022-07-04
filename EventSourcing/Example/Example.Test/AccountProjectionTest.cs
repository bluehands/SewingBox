using System.Collections.Immutable;
using EventSourcing.Events;
using Example.Domain.Events;
using Example.Domain.Projections;
using FluentAssertions;
using FunicularSwitch.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StreamIds = Example.Domain.Events.StreamIds;

namespace Example.Test;

[TestClass]
public class AccountProjectionTest
{
	[TestMethod]
	public void WhenTransferringMoneyFromOneAccountToAnother()
	{
		const string account1 = "Account_1";
		const string account2 = "Account_2";
		var myEventSequence = new AccountPayload []
		{
			new AccountCreated(account1, "Owner1", 100),
			new AccountCreated(account2, "Owner2", 200),
			new PaymentMade(account1, 20),
			new PaymentReceived(account2, 20),
		};

		var accountsById = Project(myEventSequence);

		accountsById[StreamIds.Account(account1)].Balance.Should().Be(80);
		accountsById[StreamIds.Account(account2)].Balance.Should().Be(220);
	}

	static ImmutableDictionary<StreamId, Account> Project(IEnumerable<AccountPayload> myEventSequence)
	{
		var projection = TestHost.Instance.Services.GetRequiredService<AccountProjection>();

		var accountsById = myEventSequence
			.Aggregate(
				ImmutableDictionary<StreamId, Account>.Empty,
				(accounts, @event) =>
				{
					var updated = projection.Apply(accounts.TryGetValue(@event.StreamId), @event);
					return updated.Match(u => accounts.SetItem(@event.StreamId, u),
						() => accounts.Remove(@event.StreamId));
				});
		return accountsById;
	}
}