using System.Collections.Immutable;
using EventSourcing;
using EventSourcing.Commands;
using EventSourcing.Events;
using Example.Domain.Commands;
using Example.Domain.Events;
using Example.Domain.Projections;
using Example.Host;
using FluentAssertions;
using FunicularSwitch.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Example.Domain.Events.StreamIds;

namespace Example.Test;

public class TestHost
{
	public IServiceProvider Services { get; }

	public TestHost()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection.AddExampleApp(Persistence.SqlStreamStore(StreamStoreDemoOptions.InMemory))
			.AddLogging(builder => builder.AddConsole());

		Services = serviceCollection.BuildServiceProvider();
		Services.UseEventSourcing();
	}

	public static readonly TestHost Instance = new();
}

[TestClass]
public class CreateAccountTest
{
	[TestMethod]
	public async Task EndToEnd()
	{
		var host = TestHost.Instance;

		var commandStream = host.Services.GetRequiredService<CommandStream>();
		var accounts = host.Services.GetRequiredService<Accounts>();

		await commandStream.SendCommandAndWaitUntilApplied(new CreateAccount(Guid.NewGuid().ToString(), "Alex", 1000), accounts.CommandProcessedStream);
		var allAccounts = (await accounts.GetAll()).ToImmutableArray();
		allAccounts.Should().HaveCount(1);
		var myAccount = allAccounts.Single();
		myAccount.Balance.Should().Be(1000);
	}
}

[TestClass]
public class AccountProjectionTest
{
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

		accountsById[Account(account1)].Balance.Should().Be(80);
		accountsById[Account(account2)].Balance.Should().Be(220);
	}
}