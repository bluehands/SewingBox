using EventSourcing;
using EventSourcing.Commands;
using EventSourcing.Persistence.SQLite;
using Example.Domain.Commands;
using Example.Domain.Projections;
using Example.Host;
using FunicularSwitch;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using var host = Host.CreateDefaultBuilder()
	.ConfigureServices(serviceCollection =>
	{
		//var persistenceOption = Persistence.MsSqlStreamStore(StreamStoreDemoOptions.LocalSqlExpress);
		var persistenceOption = Persistence.SQLite(@"DataSource=c:\temp\es.db");

		serviceCollection.AddExampleApp(persistenceOption);
		serviceCollection.AddTransient<SampleService>();
	})
	.ConfigureLogging(builder => builder.AddConsole())
	.Build();

host.Services.GetRequiredService<SQLiteExecutor>().AssertEventSchema();

host.Services.GetRequiredService<Accounts>().AppliedEventStream.Subscribe(t =>
	Console.WriteLine($"Balance of {t.projection.Owner}s account changed: {t.projection.Balance}"));
host.Services.UseEventSourcing();

var output = await host.Services.GetRequiredService<SampleService>()
	.CreateAccountsAndTransferMoney()
	.Match(
		ok => "Money transferred",
		error => $"Something went wrong: {error}"
	);
Console.WriteLine(output);

Console.ReadKey();


class SampleService
{
	readonly Func<Command, Task<OperationResult<Unit>>> _executeCommandAndWait;

	public SampleService(Accounts accounts, ExecuteCommandAndWaitUntilApplied executeCommandAndWait)
	{
		_executeCommandAndWait = command => executeCommandAndWait(command, accounts.CommandProcessedStream);
	}

	public async Task<OperationResult<Unit>> CreateAccountsAndTransferMoney()
	{
		var myAccount = Guid.NewGuid().ToString();
		var yourAccount = Guid.NewGuid().ToString();
		var results = await Task.WhenAll(
			_executeCommandAndWait(new CreateAccount(myAccount, "Alex", 1000)),
			_executeCommandAndWait(new CreateAccount(yourAccount, "Mace", 1000))
		);
		return await results
			.Aggregate()
			.Bind(_ => _executeCommandAndWait(new TransferMoney(myAccount, yourAccount, 123)));
	}
}