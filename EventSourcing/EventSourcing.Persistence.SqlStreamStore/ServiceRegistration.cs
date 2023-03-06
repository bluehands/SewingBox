using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using EventSourcing.Events;
using EventSourcing.Internals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlStreamStore;

namespace EventSourcing.Persistence.SqlStreamStore;

public static class ServiceRegistration
{
	public static IServiceCollection AddSqlStreamEventStore(this IServiceCollection services, SqlStreamEventStoreOptions? options = null)
	{
		options ??= new(
			CreateStore: _ => new InMemoryStreamStore(),
			CreateSerializer: _ => new JsonEventSerializer(),
			PollingOptions: PollingOptions.UsePolling(
				pollStrategy: new PeriodicObservable.RetryNTimesPollStrategy<Event, long>(e => e.Position, 10, l => l + 1),
				minPollInterval: TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(2)),
			GetLastProcessedEventPosition: () => Task.FromResult(-1L)
		);

		if (options.PollingOptions is PollingOptions.UsePolling_ polling)
		{
			services.AddSingleton(provider => new WakeUp(polling.MinPollInterval, polling.MaxPollInterval, provider.GetRequiredService<ILogger<Event>>()));
		}

		if (options.CreateStore != null)
			services.AddSingleton(options.CreateStore);
		services.AddTransient<SqlStreamStoreEventReader>();

		services.AddEventSourcing(new StreamStoreServices(), options);
		return services;
	}

	class StreamStoreServices : IEventStoreServiceRegistration<SqlStreamEventStoreOptions>
	{
		public EventStream<Event> BuildEventStream(IServiceProvider serviceProvider, SqlStreamEventStoreOptions options)
		{
			var eventReader = serviceProvider.GetRequiredService<SqlStreamStoreEventReader>();

			return options.PollingOptions
				.Match(usePolling: pollOptions =>
				{
					var wakeUp = serviceProvider.GetRequiredService<WakeUp>();

					return EventStream.CreateWithPolling(
						getLastProcessedEventNr: options.GetLastProcessedEventPosition,
						getEventNr: e => e.Position,
						getOrderedNewEvents: versionExclusive => eventReader.ReadEvents(versionExclusive + 1),
						wakeUp: wakeUp,
						getEvents: Task.FromResult,
						serviceProvider.GetRequiredService<ILogger<Event>>(),
						pollOptions.PollStrategy
					);
				}, noPolling: _ =>
				{
					var eventStore = serviceProvider.GetRequiredService<IStreamStore>();

					var existingEvents = Observable.Create<Event>(async (observer, _) =>
					{
						var lastProcessedVersion = await options.GetLastProcessedEventPosition();
						var allEvents = await eventReader.ReadEvents(lastProcessedVersion + 1);
						foreach (var @event in allEvents) observer.OnNext(@event);
						observer.OnCompleted();
					});

					var futureEvents = new System.Reactive.Subjects.Subject<Event>();
					eventStore.SubscribeToAll(null,
						async (_, message, _) => futureEvents.OnNext(await eventReader.ToEvent(message)),
						(_, reason, exception) =>
							serviceProvider.GetRequiredService<ILogger<Event>>().LogCritical($"Event subscription dropped: {reason}, {exception}. No further event will be received")
					);

					return EventStream.Create(existingEvents.Concat(futureEvents));
				});
		}

		public void AddEventReader(IServiceCollection services, SqlStreamEventStoreOptions options) => 
			services.AddTransient<IEventReader, SqlStreamStoreEventReader>();

		public void AddEventWriter(IServiceCollection services, SqlStreamEventStoreOptions options) => 
			services.AddTransient<IEventWriter, SqlStreamStoreEventWriter>();

		public void AddEventSerializer(IServiceCollection services, SqlStreamEventStoreOptions options)
		{
			if (options.CreateSerializer != null)
				services.AddTransient(options.CreateSerializer);
		}
	}
}

public class JsonEventSerializer : IEventSerializer<string>
{
	public string Serialize(object serializablePayload) => System.Text.Json.JsonSerializer.Serialize(serializablePayload);

	public object Deserialize(Type serializablePayloadType, string serializedPayload) => System.Text.Json.JsonSerializer.Deserialize(serializedPayload, serializablePayloadType)!;
}