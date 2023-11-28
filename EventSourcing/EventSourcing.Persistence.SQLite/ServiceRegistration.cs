using System;
using System.Threading.Tasks;
using EventSourcing.Events;
using EventSourcing.Internals;
using EventSourcing.Persistence.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace EventSourcing.Persistence.SQLite;

public record SQLiteEventStoreOptions(
	string ConnectionString,
	TimeSpan MinPollInterval,
	TimeSpan MaxPollInterval,
	PeriodicObservable.PollStrategy<Event, long> PollStrategy)
{
	public static SQLiteEventStoreOptions Create(
		string connectionString, 
		TimeSpan? minPollInterval = null, 
		TimeSpan? maxPollInterval = null, 
		PeriodicObservable.PollStrategy<Event, long>? pollStrategy = null) =>
		new(
			connectionString,
			minPollInterval ?? TimeSpan.FromMilliseconds(100),
			maxPollInterval ?? TimeSpan.FromSeconds(30),
			pollStrategy ?? new PeriodicObservable.RetryNTimesPollStrategy<Event, long>(e => e.Position, 10, position => position + 1)
		);
}

public static class ServiceRegistration
{
	public static IServiceCollection AddSQLiteEventStore(this IServiceCollection services, SQLiteEventStoreOptions options)
	{
		services.AddSingleton(_ => new SQLiteExecutor(options.ConnectionString));
		services.AddSingleton<SQLiteEventStore>();

		services.AddSingleton<WakeUp>(provider => new(options.MinPollInterval, options.MaxPollInterval, provider.GetRequiredService<ILogger<Event>>()));
		
		services.AddEventSourcing(new EventStoreServices(), options);

		return services;
	}

	class EventStoreServices : IEventStoreServiceRegistration<SQLiteEventStoreOptions>
	{
		public EventStream<Event> BuildEventStream(IServiceProvider provider, SQLiteEventStoreOptions options)
		{
			var eventStore = provider.GetRequiredService<SQLiteEventStore>();
			var wakeUp = provider.GetRequiredService<WakeUp>();

			return EventStream.CreateWithPolling(
				getLastProcessedEventNr: () => Task.FromResult(-1L),
				getEventNr: e => e.Position,
				getOrderedNewEvents: fromPositionExclusive => eventStore.ReadEvents(fromPositionExclusive + 1),
				wakeUp: wakeUp,
				getEvents: Task.FromResult,
				provider.GetRequiredService<ILogger<Event>>(),
				options.PollStrategy
			);
		}

		public void AddEventReader(IServiceCollection services, SQLiteEventStoreOptions options)
			=> services.AddTransient<IEventReader>(provider => provider.GetRequiredService<SQLiteEventStore>());

		public void AddEventWriter(IServiceCollection services, SQLiteEventStoreOptions options) => 
			services.AddTransient<IEventWriter>(provider => provider.GetRequiredService<SQLiteEventStore>());

		public void AddEventSerializer(IServiceCollection services, SQLiteEventStoreOptions options) => 
			services.TryAddTransient<IEventSerializer<string>, JsonEventSerializer>();
	}
}