using EventSourcing;
using EventSourcing.Example.Domain;
using EventSourcing.Persistence.InMemory;
using FunicularSwitch;
using FunicularSwitch.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using var host = Host.CreateDefaultBuilder()
	.ConfigureServices(serviceCollection =>
	{
		var payloadAssemblies = typeof(AccountCreated).Assembly.Yield();
		var commandProcessorAssemblies = typeof(CreateAccountCommandProcessor).Assembly.Yield();
		serviceCollection
			.AddEventSourcing(payloadAssemblies, commandProcessorAssemblies)
			.AddInMemoryEventStore();

		serviceCollection.AddSingleton<Accounts>();
		serviceCollection.AddTransient<SampleApp>();
	})
	.ConfigureLogging(builder => builder.AddConsole())
	.Build();

host.Services.GetRequiredService<Accounts>().AppliedEventStream.Subscribe(t =>
	Console.WriteLine($"Balance of {t.projection.Owner}s account changed: {t.projection.Balance}"));
host.Services.UseEventSourcing();

var output = await host.Services.GetRequiredService<SampleApp>()
	.CreateAccountsAndTransferMoney()
	.Match(
		ok => "Money transferred",
		error => $"Something went wrong: {error}"
	);
Console.WriteLine(output);

Console.ReadKey();

class SampleApp
{
	readonly Func<Command, Task<OperationResult<Unit>>> _executeCommandAndWait;

	public SampleApp(Accounts accounts, ExecuteCommandAndWaitUntilApplied executeCommandAndWait)
	{
		_executeCommandAndWait = command => executeCommandAndWait(command, accounts.CommandProcessedStream);
	}

	public async Task<OperationResult<Unit>> CreateAccountsAndTransferMoney()
	{
		var myAccount = Guid.NewGuid().ToString();
		var yourAccount = Guid.NewGuid().ToString();
		var results = await Task.WhenAll(
			_executeCommandAndWait(new CreateAccount(myAccount, "Alex", 0)),
			_executeCommandAndWait(new CreateAccount(yourAccount, "Mace", 1000))
		);
		return await results
			.Aggregate()
			.Bind(_ => _executeCommandAndWait(new ExecuteTransaction(myAccount, yourAccount, 123)));
	}
}