using System.Reactive.Linq;
using EventSourcing;
using Example.Domain.Events;
using Example.Domain.Projections;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using StreamIds = Example.Domain.Events.StreamIds;

namespace Example.Test;

[TestClass]
public class AccountProjectionTest
{
	[TestMethod]
	public async Task WhenTransferringMoneyFromOneAccountToAnother()
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

		var accounts = TestHost.Instance.Services.GetRequiredService<Accounts>();
		var allApplied = accounts.AppliedEventStream.Take(myEventSequence.Length);
		await TestHost.Instance.Services.GetRequiredService<IEventWriter>().WriteEvents(myEventSequence);
		await allApplied;

		(await GetAccount(account1)).Balance.Should().Be(80);
		(await GetAccount(account2)).Balance.Should().Be(220);

		async Task<Account> GetAccount(string account) => (await accounts.Get(StreamIds.Account(account))).GetValueOrThrow();
	}
}