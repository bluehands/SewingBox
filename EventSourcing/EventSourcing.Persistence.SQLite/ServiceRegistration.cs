using System;
using System.Threading.Tasks;
using EventSourcing.Events;
using EventSourcing.Internals;
using EventSourcing.Persistence.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventSourcing.Persistence.SQLite;

public record SQLiteEventStoryOptions(
	string ConnectionString, 
	TimeSpan MinPollInterval, 
	TimeSpan MaxPollInterval, 
	PeriodicObservable.PollStrategy<Event, long> PollStrategy,
	Func<IServiceProvider, IEventSerializer<string>>? CreateSerializer);


public static class ServiceRegistration
{
	public static IServiceCollection AddSQLiteEventStore(this IServiceCollection services, SQLiteEventStoryOptions? options = null)
	{
		var storeOptions = options ?? new SQLiteEventStoryOptions(
			"file::memory:?cache=shared",
			TimeSpan.FromMilliseconds(100),
			TimeSpan.FromSeconds(30),
			new PeriodicObservable.RetryForeverPollStrategy<Event, long>(e => e.Position),
			_ => new JsonEventSerializer()
		);

		services.AddSingleton(_ => new SQLiteExecutor(storeOptions.ConnectionString));
		services.AddSingleton<SQLiteEventStore>();

		services.AddSingleton<WakeUp>(provider => new(storeOptions.MinPollInterval, storeOptions.MaxPollInterval, provider.GetRequiredService<ILogger<Event>>()));
		
		services.AddEventSourcing(new EventStoreServices(), storeOptions);

		return services;
	}

	class EventStoreServices : IEventStoreServiceRegistration<SQLiteEventStoryOptions>
	{
		public EventStream<Event> BuildEventStream(IServiceProvider provider, SQLiteEventStoryOptions options)
		{
			var inMemoryEventStore = provider.GetRequiredService<SQLiteEventStore>();
			var wakeUp = provider.GetRequiredService<WakeUp>();

			return EventStream.CreateWithPolling(
				getLastProcessedEventNr: () => Task.FromResult(-1L),
				getEventNr: e => e.Position,
				getOrderedNewEvents: fromPositionExclusive => inMemoryEventStore.ReadEvents(fromPositionExclusive + 1),
				wakeUp: wakeUp,
				getEvents: Task.FromResult,
				provider.GetRequiredService<ILogger<Event>>(),
				options.PollStrategy
			);
		}

		public void AddEventReader(IServiceCollection services, SQLiteEventStoryOptions options)
		{
			services.AddTransient<IEventReader>(provider => provider.GetRequiredService<SQLiteEventStore>());
		}

		public void AddEventWriter(IServiceCollection services, SQLiteEventStoryOptions options)
		{
			services.AddTransient<IEventWriter>(provider => provider.GetRequiredService<SQLiteEventStore>());
		}

		public void AddEventSerializer(IServiceCollection services, SQLiteEventStoryOptions options)
		{
			if (options.CreateSerializer != null)
				services.AddTransient(options.CreateSerializer);
		}
	}
}