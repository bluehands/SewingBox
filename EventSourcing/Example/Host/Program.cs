using EventSourcing;
using EventSourcing.Commands;
using EventSourcing.JsonPayloads;
using EventSourcing.Persistence.InMemory;
using EventSourcing.Persistence.SqlStreamStore;
using Example.Domain.Commands;
using Example.Domain.Projections;
using Example.Host;
using Example.JsonPayloads;
using FunicularSwitch;
using FunicularSwitch.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using AccountCreated = Example.Domain.Events.AccountCreated;

var persistenceDemoOption = Persistence.InMemoryNoMappers;

var payloadAssemblies = typeof(AccountCreated).Assembly.Yield();
var commandProcessorAssemblies = typeof(CreateAccountCommandProcessor).Assembly.Yield();

using var host = Host.CreateDefaultBuilder()
	.ConfigureServices(serviceCollection =>
	{
		persistenceDemoOption.Match(
			inMemoryNoMappers: _ => 
				serviceCollection
					.AddEventSourcing(payloadAssemblies, commandProcessorAssemblies)
					.AddInMemoryEventStore(),
			sqlStreamStore: _ =>
			{
				var payloadMapperAssemblies =
					new[] { typeof(AccountCreatedMapper), typeof(CommandProcessedMapper) }.Select(t => t.Assembly);

				return serviceCollection
					.AddEventSourcing(payloadAssemblies, commandProcessorAssemblies, payloadMapperAssemblies)
					.AddSqlStreamEventStore();
			}
		);

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

namespace Example.Host
{
	class SampleApp
	{
		readonly Func<Command, Task<EventSourcing.OperationResult<Unit>>> _executeCommandAndWait;

		public SampleApp(Accounts accounts, ExecuteCommandAndWaitUntilApplied executeCommandAndWait)
		{
			_executeCommandAndWait = command => executeCommandAndWait(command, accounts.CommandProcessedStream);
		}

		public async Task<EventSourcing.OperationResult<Unit>> CreateAccountsAndTransferMoney()
		{
			var myAccount = Guid.NewGuid().ToString();
			var yourAccount = Guid.NewGuid().ToString();
			var results = await Task.WhenAll(
				_executeCommandAndWait(new CreateAccount(myAccount, "Alex", 0)),
				_executeCommandAndWait(new CreateAccount(yourAccount, "Mace", 1000))
			);
			return await results
				.Aggregate()
				.Bind(_ => _executeCommandAndWait(new TransferMoney(myAccount, yourAccount, 123)));
		}
	}

	[FunicularSwitch.Generators.UnionType(CaseOrder = FunicularSwitch.Generators.CaseOrder.AsDeclared)]
	public abstract class Persistence
	{
		public static readonly Persistence InMemoryNoMappers = new InMemoryNoMappers_();
		public static readonly Persistence SqlStreamStore = new SqlStreamStore_();

		public class InMemoryNoMappers_ : Persistence
		{
			public InMemoryNoMappers_() : base(UnionCases.InMemoryNoMappers)
			{
			}
		}

		public class SqlStreamStore_ : Persistence
		{
			public SqlStreamStore_() : base(UnionCases.SqlStreamStore)
			{
			}
		}

		internal enum UnionCases
		{
			InMemoryNoMappers,
			SqlStreamStore
		}

		internal UnionCases UnionCase { get; }
		Persistence(UnionCases unionCase) => UnionCase = unionCase;

		public override string ToString() => Enum.GetName(typeof(UnionCases), UnionCase) ?? UnionCase.ToString();
		bool Equals(Persistence other) => UnionCase == other.UnionCase;

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((Persistence)obj);
		}

		public override int GetHashCode() => (int)UnionCase;
	}
}