using EventSourcing.Example.Domain.Events;
using EventSourcing.Example.Domain.Projections;

namespace EventSourcing.Example.Domain.Commands;

public record ExecuteTransaction(string FromAccount, string ToAccount, decimal Amount) : Command;

public class ExecuteTransactionCommandProcessor : CommandProcessor<ExecuteTransaction>
{
	readonly Accounts _accounts;

	public ExecuteTransactionCommandProcessor(Accounts accounts) => _accounts = accounts;

	public override async Task<ProcessingResult.Processed_> InternalProcess(ExecuteTransaction command)
	{
		IEnumerable<Failure> SenderHasEnoughMoney(Account account)
		{
			if (account.Balance < command.Amount)
				yield return Failure.InvalidInput("Sender does not have enough money");
		}

		var fromAccount = (await _accounts.Get(Events.StreamIds.Account(command.FromAccount)))
			.ToResult(() => $"Source account {command.FromAccount} does not exist")
			.Bind(fromAccount => fromAccount.Validate(SenderHasEnoughMoney));
		var toAccount = (await _accounts.Get(Events.StreamIds.Account(command.ToAccount)))
			.ToResult(() => $"Target account {command.FromAccount} does not exist");

		return fromAccount
			.Aggregate(toAccount, (from, to) => (from, to))
			.Map(t => (IReadOnlyCollection<EventPayload>)new EventPayload[]
				{ new PaymentMade(t.from.Id, command.Amount), new PaymentReceived(t.to.Id, command.Amount) })
			.ToProcessedResult(command,
				_ => $"Transferred {command.Amount} € from {command.FromAccount} to {command.ToAccount}");
	}
}