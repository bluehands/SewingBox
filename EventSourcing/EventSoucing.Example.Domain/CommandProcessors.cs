namespace EventSourcing.Example.Domain;

public record CreateAccount(string AccountId, string Owner, decimal InitialBalance) : Command;

public class CreateAccountCommandProcessor : SynchronousCommandProcessor<CreateAccount>
{
	public override ProcessingResult.Processed_ InternalProcessSync(CreateAccount command) => command.ToProcessedResult(
		new AccountCreated(command.AccountId, command.Owner, command.InitialBalance),
		FunctionalResult.Ok($"Account {command.AccountId} create for {command.Owner}"));
}

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

		var fromAccount = (await _accounts.Get(StreamIds.Account(command.FromAccount)))
			.ToResult(() => $"Source account {command.FromAccount} does not exist")
			.Bind(fromAccount => fromAccount.Validate(SenderHasEnoughMoney));
		var toAccount = (await _accounts.Get(StreamIds.Account(command.ToAccount)))
			.ToResult(() => $"Target account {command.FromAccount} does not exist");

		return fromAccount
			.Aggregate(toAccount, (from, to) => (from, to))
			.Map(t => (IReadOnlyCollection<EventPayload>)new EventPayload[]
				{ new PaymentMade(t.from.Id, command.Amount), new PaymentReceived(t.to.Id, command.Amount) })
			.ToProcessedResult(command, _ => $"Transferred {command.Amount} € from {command.FromAccount} to {command.ToAccount}");
	}
}

public static class OperationResultExtension
{
	public static OperationResult<T> ToResult<T>(this FunicularSwitch.Option<T> option, Func<string> errorOnEmpty) =>
		option.Match(some => some, () => OperationResult.InvalidInput<T>(errorOnEmpty()));
}