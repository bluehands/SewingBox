using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using EventSourcing.Events;
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
			CreateSerializer: _ => new JsonSerializer(),
			UsePolling: true,
			GetLastProcessedEventVersion: () => Task.FromResult(-1L)
		);

		services.AddSingleton(options.CreateStore);
		services.AddTransient(options.CreateSerializer);

		if (options.UsePolling)
		{
			services.AddSingleton(serviceProvider =>
			{
				var eventReader = serviceProvider.GetRequiredService<StreamStoreEventReader>();
				return EventStream.CreateWithPolling(
					getLastProcessedEventNr: options.GetLastProcessedEventVersion,
					getEventNr: e => e.Version,
					getOrderedNewEvents: versionExclusive => eventReader.ReadEvents(versionExclusive + 1),
					pollInterval: TimeSpan.FromMilliseconds(100),
					getEvents: e => Task.FromResult(e),
					serviceProvider.GetRequiredService<ILogger<Event>>()
				);
			});
		}
		else
		{
			services.AddSingleton(serviceProvider =>
			{
				var eventReader = serviceProvider.GetRequiredService<StreamStoreEventReader>();
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

				return new EventStream<Event>(existingEvents.Concat(futureEvents));
			});
		}

		services.AddSingleton<IObservable<Event>>(provider => provider.GetRequiredService<EventStream<Event>>());

		
		services.AddTransient<IEventWriter, StreamStoreEventWriter>();
		services.AddTransient<StreamStoreEventReader>();
		services.AddTransient<IEventReader, StreamStoreEventReader>();

		//TODO: move to eventsourcing
		services.AddSingleton<WriteEvents>(serviceProvider => payloads => serviceProvider.GetRequiredService<IEventWriter>().WriteEvents(payloads));
		services.AddSingleton<LoadAllEvents>(serviceProvider => () => serviceProvider.GetRequiredService<IEventReader>().LoadAllEvents());

		services.AddSingleton<LoadEventsByStreamId>(serviceProvider => 
			(streamId, upToVersionExclusive) => serviceProvider.GetRequiredService<IEventReader>().LoadEventsByStreamId(streamId, upToVersionExclusive));
	}
}

public class JsonSerializer : IEventSerializer<string>
{
	public string Serialize(object serializablePayload) => System.Text.Json.JsonSerializer.Serialize(serializablePayload);

	public object Deserialize(Type serializablePayloadType, string serializedPayload) => System.Text.Json.JsonSerializer.Deserialize(serializedPayload, serializablePayloadType);
}