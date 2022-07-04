using System.Collections.Immutable;
using EventSourcing.Commands;
using Example.Domain.Commands;
using Example.Domain.Projections;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Example.Test;

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