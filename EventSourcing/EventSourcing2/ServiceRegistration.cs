using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using EventSourcing.Commands;
using EventSourcing2.Commands;
using EventSourcing2.Events;
using EventSourcing2.Internals;
using FunicularSwitch.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventSourcing2;

public static class StartUpExtensions
{
	public static void UseEventSourcing(this IServiceProvider serviceProvider)
	{
		serviceProvider.SubscribeCommandProcessors();
		serviceProvider.GetRequiredService<EventStream<Event>>().Start();
	}
}

public static class ServiceCollectionExtension
{
    public static IServiceCollection AddEventSourcing(this IServiceCollection serviceCollection, Action<EventSourcingOptionsBuilder> configure)
    {
        var builder = EventSourcingOptionsBuilder.WithCoreOptions();
        configure(builder);

		foreach (var eventSourcingOptionsExtension in builder.Options.Extensions)
        {
            eventSourcingOptionsExtension.ApplyServices(serviceCollection);
        }

        return serviceCollection;
    }


	public static IServiceCollection AddEventSourcing<TOptions, TDbEvent, TSerializedPayload>(
        this IServiceCollection services, 
        IEventStoreServiceRegistration<TOptions> eventStoreServices,
        TOptions options) where TOptions : EventStoreOptions
	{
		eventStoreServices.AddEventReader(services, options);
		eventStoreServices.AddEventWriter(services, options);
		eventStoreServices.AddEventSerializer(services, options);
		eventStoreServices.AddDbEventDescriptor(services);
        eventStoreServices.AddEventStream(services, options);

		var entryAssembly = Assembly.GetEntryAssembly();
        return services
            .AddSingleton<IObservable<Event>>(provider => provider.GetRequiredService<EventStream<Event>>())
            .AddTransient<IEventStore, EventStore<TDbEvent, TSerializedPayload>>()
            .AddTransient<IEventMapper<TDbEvent>, EventStore<TDbEvent, TSerializedPayload>>()
            .RegisterEventPayloads(options.PayloadAssemblies ?? entryAssembly.Yield())
            .RegisterPayloadMappers(options.PayloadMapperAssemblies ?? entryAssembly.Yield());
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

		//TODO:
		//serviceCollection.AddSingleton<ExecuteCommand>(provider => provider.GetRequiredService<CommandStream>().SendCommand);
		//serviceCollection.AddSingleton<ExecuteCommandAndWaitUntilApplied>(provider => (command, processedStream) =>  provider.GetRequiredService<CommandStream>().SendCommandAndWaitUntilApplied(command, processedStream));
		//serviceCollection.AddSingleton<EventStreamFactory>();

		return serviceCollection;
	}


	public static IServiceCollection RegisterEventPayloads(this IServiceCollection serviceCollection, IEnumerable<Assembly> payloadAssemblies)
	{
		EventFactory.Initialize(payloadAssemblies);
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
			serviceCollection.Add(ServiceDescriptor.Describe(serviceType, implementationType, ServiceLifetime.Transient));

		return serviceCollection;
	}

	public static CommandProcessorSubscription SubscribeCommandProcessors(this IServiceProvider provider)
	{
		var commandStream = provider.GetRequiredService<CommandStream>();
		var logger = provider.GetRequiredService<ILogger<Event>>();
		var wakeUp = provider.GetService<WakeUp>();
		var eventStore = provider.GetRequiredService<IEventStore>();

		var subscription = commandStream
			.SubscribeCommandProcessors(
				commandType =>
				{
                    var commandProcessorType = typeof(CommandProcessor<>).MakeGenericType(commandType);
					try
					{
						var scope = provider.CreateScope();
						var processor = (CommandProcessor)scope.ServiceProvider.GetService(commandProcessorType);
                        return new(processor, scope);
                    }
					catch (Exception e)
					{
						provider.GetService<ILogger<Event>>()?.LogError(e,
							$"Failed to resolve command processor of type {commandProcessorType}");
					}

					return null;
				},
                eventStore, logger, wakeUp);
		return new(subscription);
	}
}

public sealed class CommandProcessorSubscription(IDisposable subscription) : IDisposable
{
    public void Dispose() => subscription.Dispose();
}

public class EventStoreOptions
{
    public IReadOnlyCollection<Assembly>? PayloadAssemblies { get; set; }
    public IReadOnlyCollection<Assembly>? PayloadMapperAssemblies { get; set; }
    public long EventStreamStartingPosition { get; set; }
}