using EventSourcing;
using EventSourcing.JsonPayloads;
using EventSourcing.Persistence.InMemory;
using EventSourcing.Persistence.SQLite;
using EventSourcing.Persistence.SqlStreamStore;
using Example.Domain.Commands;
using Example.Domain.Projections;
using Example.JsonPayloads;
using FunicularSwitch.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SqlStreamStore;
using AccountCreated = Example.Domain.Events.AccountCreated;

namespace Example.Host;

public static class ServiceRegistration
{
	public static IServiceCollection AddExampleApp(this IServiceCollection serviceCollection, Persistence persistenceOption)
	{
		var payloadAssemblies = typeof(AccountCreated).Assembly.Yield();
		var commandProcessorAssemblies = typeof(CreateAccountCommandProcessor).Assembly.Yield();
		var payloadMapperAssemblies = new[] { typeof(AccountCreatedMapper), typeof(CommandProcessedMapper) }.Select(t => t.Assembly);

		serviceCollection.AddEventSourcing(payloadAssemblies, commandProcessorAssemblies, payloadMapperAssemblies);
		persistenceOption.Match(
			inMemoryNoMappers: _ => serviceCollection.AddInMemoryEventStore(),
			msSqlStreamStore: sqlStreamStore => serviceCollection
				.AddSingleton<IStreamStore>(_ =>
				{
					var store = new MsSqlStreamStoreV3(new(sqlStreamStore.ConnectionString));
					//this should be called on app startup in async context. It's here to have everything in one place for demo purposes
					store.CreateSchemaIfNotExists().GetAwaiter().GetResult();
					return store;
				})
				.AddSqlStreamEventStore(),
			sqlStreamStoreInMemory: _ => serviceCollection.AddSqlStreamEventStore(),
			sQLite: sQLite => serviceCollection.AddSQLiteEventStore(SQLiteEventStoreOptions.Create(sQLite.ConnectionString)));

		serviceCollection
			.AddTransient<AccountProjection>()
			.AddSingleton<Accounts>();

		return serviceCollection;
	}
}