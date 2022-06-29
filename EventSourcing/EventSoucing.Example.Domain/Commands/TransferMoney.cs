using EventSourcing.Commands;
using EventSourcing.Events;
using EventSourcing.Example.Domain.Events;
using EventSourcing.Example.Domain.Projections;

namespace EventSourcing.Example.Domain.Commands;

public record TransferMoney(string FromAccount, string ToAccount, decimal Amount) : Command;

public class TransferMoneyCommandProcessor : CommandProcessor<TransferMoney>
{
	readonly Accounts _accounts;

	public TransferMoneyCommandProcessor(Accounts accounts) => _accounts = accounts;

	public override async Task<ProcessingResult.Processed_> InternalProcess(TransferMoney command)
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
			.Map(t => new EventPayload[]
			{
				new PaymentMade(t.from.Id, command.Amount), 
				new PaymentReceived(t.to.Id, command.Amount)
			})
			.ToProcessedResultMulti(command, _ => $"Transferred {command.Amount} € from {command.FromAccount} to {command.ToAccount}");
	}
}