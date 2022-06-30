using EventSourcing;
using EventSourcing.Commands;
using Example.Domain.Commands;
using Example.Domain.Projections;
using JetBrains.Annotations;

namespace Example.Host.GraphQl;

[UsedImplicitly]
public class Mutation
{
	readonly CommandStream _commandStream;
	readonly Accounts _accounts;

	public Mutation(CommandStream commandStream, Accounts accounts)
	{
		_commandStream = commandStream;
		_accounts = accounts;
	}

	public Task<string> CreateAccount(string owner, decimal initialBalance)
	{
		var accountId = Guid.NewGuid().ToString();
		var result = _commandStream.SendCommandAndWaitUntilApplied(new CreateAccount(Guid.NewGuid().ToString(), owner, initialBalance), _accounts.CommandProcessedStream);
		return result.Match(ok => $"Account {accountId} created for {owner}", error => throw new GraphQLException(error.Message));
	}

	public async Task<string> SendMoney(string fromAccount,  string toAccount, decimal amount)
	{
		var result = await _commandStream
			.SendCommandAndWaitUntilApplied(new TransferMoney(fromAccount, toAccount, amount), _accounts.CommandProcessedStream);

		return result.Match(ok => "Tranferred!", error => throw new GraphQLException(error.Message));
	}
}