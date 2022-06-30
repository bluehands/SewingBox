using EventSourcing;
using EventSourcing.JsonPayloads;
using EventSourcing.Persistence.InMemory;
using EventSourcing.Persistence.SqlStreamStore;
using Example.Domain.Commands;
using Example.Domain.Projections;
using Example.JsonPayloads;
using FunicularSwitch.Extensions;
using Microsoft.Extensions.DependencyInjection;
using AccountCreated = Example.Domain.Events.AccountCreated;

namespace Example.Host;

public static class ServiceRegistration
{
	public static IServiceCollection AddExampleApp(this IServiceCollection serviceCollection, Persistence persistenceOption)
	{
		var payloadAssemblies = typeof(AccountCreated).Assembly.Yield();
		var commandProcessorAssemblies = typeof(CreateAccountCommandProcessor).Assembly.Yield();

		persistenceOption.Match(
			inMemoryNoMappers: _ =>
				serviceCollection
					.AddEventSourcing(payloadAssemblies, commandProcessorAssemblies)
					.AddInMemoryEventStore(),
			sqlStreamStore: sqlStreamStore =>
			{
				var payloadMapperAssemblies =
					new[] { typeof(AccountCreatedMapper), typeof(CommandProcessedMapper) }.Select(t => t.Assembly);

				return serviceCollection
					.AddEventSourcing(payloadAssemblies, commandProcessorAssemblies, payloadMapperAssemblies)
					.AddSqlStreamEventStore(sqlStreamStore.Options);
			}
		);

		serviceCollection
			.AddTransient<AccountProjection>()
			.AddSingleton<Accounts>();

		return serviceCollection;
	}
}