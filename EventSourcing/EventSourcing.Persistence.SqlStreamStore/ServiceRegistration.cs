using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using EventSourcing.Events;
using EventSourcing.Internals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlStreamStore;

namespace EventSourcing.Persistence.SqlStreamStore;

public record EventStoreOptions(Func<IServiceProvider, IStreamStore> CreateStore, Func<IServiceProvider, IEventSerializer<string>> CreateSerializer, bool UsePolling, Func<Task<long>> GetLastProcessedEventVersion);

public static class ServiceRegistration
{
	public static void AddSqlStreamEventStore(this IServiceCollection services, EventStoreOptions options = null)
	{
		options ??= new(
			CreateStore: _ => new InMemoryStreamStore(),
			CreateSerializer: _ => new JsonEventSerializer(),
			UsePolling: true,
			GetLastProcessedEventVersion: () => Task.FromResult(-1L)
		);

		services.AddSingleton(options.CreateStore);
		services.AddTransient<StreamStoreEventReader>();

		services.AddEventSourcing(new StreamStoreServices(), options);
	}

	class StreamStoreServices : IEventStoreServiceRegistration<EventStoreOptions>
	{
		public EventStream<Event> BuildEventStream(IServiceProvider serviceProvider, EventStoreOptions options)
		{
			var eventReader = serviceProvider.GetRequiredService<StreamStoreEventReader>();

			if (options.UsePolling)
			{
				return EventStream.CreateWithPolling(
					getLastProcessedEventNr: options.GetLastProcessedEventVersion,
					getEventNr: e => e.Version,
					getOrderedNewEvents: versionExclusive => eventReader.ReadEvents(versionExclusive + 1),
					pollInterval: TimeSpan.FromMilliseconds(100),
					getEvents: e => Task.FromResult(e),
					serviceProvider.GetRequiredService<ILogger<Event>>()
				);
			}

			var eventStore = serviceProvider.GetRequiredService<IStreamStore>();

			var existingEvents = Observable.Create<Event>(async (observer, _) =>
			{
				var lastProcessedVersion = await options.GetLastProcessedEventVersion();
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

			return new(existingEvents.Concat(futureEvents));
		}

		public void AddEventReader(IServiceCollection services, EventStoreOptions options)
		{
			services.AddTransient<IEventReader, StreamStoreEventReader>();
		}

		public void AddEventWriter(IServiceCollection services, EventStoreOptions options)
		{
			services.AddTransient<IEventWriter, StreamStoreEventWriter>();
		}

		public void AddEventSerializer(IServiceCollection services, EventStoreOptions options)
		{
			services.AddTransient(options.CreateSerializer);
		}
	}
}

public class JsonEventSerializer : IEventSerializer<string>
{
	public string Serialize(object serializablePayload) => System.Text.Json.JsonSerializer.Serialize(serializablePayload);

	public object Deserialize(Type serializablePayloadType, string serializedPayload) => System.Text.Json.JsonSerializer.Deserialize(serializedPayload, serializablePayloadType);
}