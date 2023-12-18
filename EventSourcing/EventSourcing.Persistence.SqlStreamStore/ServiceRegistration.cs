using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using EventSourcing.Events;
using EventSourcing.Internals;
using EventSourcing.Persistence.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SqlStreamStore;

namespace EventSourcing.Persistence.SqlStreamStore;

public static class ServiceRegistration
{
	public static IServiceCollection AddSqlStreamEventStore(this IServiceCollection services, SqlStreamEventStoreOptions? options = null)
	{
		options ??= SqlStreamEventStoreOptions.Create();

		if (options.PollingOptions is PollingOptions.UsePolling_ polling)
		{
			services.AddSingleton(provider => new WakeUp(polling.MinPollInterval, polling.MaxPollInterval, provider.GetRequiredService<ILogger<Event>>()));
		}

		services.TryAddSingleton<IStreamStore, InMemoryStreamStore>();
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
						var allEvents = await eventReader
                            .ReadEvents(lastProcessedVersion + 1);
						foreach (var @event in allEvents.GetValueOrThrow()) observer.OnNext(@event);
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

		public void AddEventSerializer(IServiceCollection services, SqlStreamEventStoreOptions options) => 
			services.TryAddTransient<IEventSerializer<string>, JsonEventSerializer>();
	}
}