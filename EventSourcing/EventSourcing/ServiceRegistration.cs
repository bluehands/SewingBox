using System.Collections.Immutable;
using System.Reflection;
using EventSourcing.Commands;
using EventSourcing.Events;
using EventSourcing.Internals;
using FunicularSwitch.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventSourcing;

public static class StartUpExtensions
{
	public static void UseEventSourcing(this IServiceProvider serviceProvider)
	{
		// this actually subscribes command processors to commands
		serviceProvider.GetRequiredService<CommandProcessorSubscription>();
		serviceProvider.GetRequiredService<EventStream<Event>>().Start();
	}
}

public static class ServiceCollectionExtension
{
	public static IServiceCollection AddEventSourcing<TOptions>(this IServiceCollection services, IEventStoreServiceRegistration<TOptions> eventStoreServices, TOptions options)
	{
		services.AddSingleton(provider => eventStoreServices.BuildEventStream(provider, options));
		eventStoreServices.AddEventReader(services, options);
		eventStoreServices.AddEventWriter(services, options);
		eventStoreServices.AddEventSerializer(services, options);

		return services
			.AddSingleton<IObservable<Event>>(provider => provider.GetRequiredService<EventStream<Event>>())
			.AddSingleton<WriteEvents>(serviceProvider => payloads => serviceProvider.GetRequiredService<IEventWriter>().WriteEvents(payloads))
			.AddSingleton<LoadAllEvents>(serviceProvider => () => serviceProvider.GetRequiredService<IEventReader>().LoadAllEvents())
			.AddSingleton<LoadEventsByStreamId>(serviceProvider => (streamId, upToVersionExclusive) => serviceProvider.GetRequiredService<IEventReader>().LoadEventsByStreamId(streamId, upToVersionExclusive));
	}

	public static IServiceCollection AddEventSourcing(this IServiceCollection serviceCollection,
		IEnumerable<Assembly> payloadAssemblies,
		IEnumerable<Assembly> commandProcessorAssemblies,
		IEnumerable<Assembly>? payloadMapperAssemblies = null)
	{
		serviceCollection
			.RegisterEventPayloads(payloadAssemblies)
			.AddCommandProcessors(commandProcessorAssemblies);

		if (payloadMapperAssemblies != null)
			serviceCollection.RegisterPayloadMappers(payloadMapperAssemblies);

		serviceCollection.AddSingleton<ExecuteCommand>(provider => provider.GetRequiredService<CommandStream>().SendCommand);
		serviceCollection.AddSingleton<ExecuteCommandAndWaitUntilApplied>(provider => (command, processedStream) =>  provider.GetRequiredService<CommandStream>().SendCommandAndWaitUntilApplied(processedStream, command));

		return serviceCollection;
	}


	public static IServiceCollection RegisterEventPayloads(this IServiceCollection serviceCollection, IEnumerable<Assembly> payloadAssemblies)
	{
		EventFactory.Initialize(payloadAssemblies.Concat(new []{typeof(CommandProcessed).Assembly}));
		return serviceCollection;
	}

	public static IServiceCollection RegisterPayloadMappers(this IServiceCollection serviceCollection, IEnumerable<Assembly> payloadMapperAssemblies)
	{
		EventPayloadMapper.Register(payloadMapperAssemblies);
		return serviceCollection;
	}

	public static IServiceCollection AddCommandProcessors(this IServiceCollection serviceCollection, IEnumerable<Assembly> commandProcessorAssemblies)
	{
		serviceCollection.AddSingleton<CommandStream>();

		var tuples = typeof(CommandProcessor)
			.GetConcreteDerivedTypes(commandProcessorAssemblies.Concat(typeof(NoOpCommandProcessor).Assembly.Yield()))
			.Select(processorType =>
			{
				var commandProcessorType = processorType.GetBaseType(t =>
					t.IsGenericType && t.GetGenericTypeDefinition() == typeof(CommandProcessor<>));
				return (ServiceType: commandProcessorType, implementationType: processorType);
			})
			.GroupBy(t => t.ServiceType, t => t.implementationType)
			.Select(g =>
			{
				var implementations = g.ToImmutableArray();
				if (implementations.Length > 1)
					throw new($"Found multiple processors for command of type {g.Key.GetGenericArguments()[0]}: {string.Join(",", implementations)}");

				return (
					ServiceType: g.Key,
					implementationType: implementations[0]
				);

			});

		foreach (var (serviceType, implementationType) in tuples)
			serviceCollection.Add(ServiceDescriptor.Describe(serviceType, implementationType, ServiceLifetime.Scoped));

		return serviceCollection.SubscribeCommandProcessors();
	}

	public static IServiceCollection SubscribeCommandProcessors(this IServiceCollection serviceCollection)
	{
		serviceCollection.AddSingleton(provider =>
		{
			var commandStream = provider.GetRequiredService<CommandStream>();
			var writeEvents = provider.GetRequiredService<WriteEvents>();

			var subscription = commandStream
				.SubscribeCommandProcessors(
					commandType =>
					{
						var commandProcessorType = typeof(CommandProcessor<>).MakeGenericType(commandType);
						try
						{
							return (CommandProcessor)provider.GetService(commandProcessorType);
						}
						catch (Exception e)
						{
							provider.GetService<ILogger<Event>>()?.LogError(e, $"Failed to resolve command processor of type {commandProcessorType}");
						}

						return null;
					},
					writeEvents, provider.GetService<ILogger<Event>>());
			return new CommandProcessorSubscription(subscription);
		});

		return serviceCollection;
	}
}

public sealed class CommandProcessorSubscription : IDisposable
{
	readonly IDisposable _subscription;

	public CommandProcessorSubscription(IDisposable subscription) => _subscription = subscription;

	public void Dispose() => _subscription.Dispose();
}