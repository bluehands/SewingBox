using System;
using System.Threading.Tasks;
using EventSourcing.Events;
using EventSourcing.Internals;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventSourcing.Persistence.InMemory;

public record InMemoryEventStoryOptions(TimeSpan MinPollInterval, TimeSpan MaxPollInterval, PeriodicObservable.PollStrategy<Event, long> PollStrategy);


public static class ServiceRegistration
{
	public static IServiceCollection AddInMemoryEventStore(this IServiceCollection services, InMemoryEventStoryOptions? options = null)
	{
		services.AddSingleton<InMemoryEventStore>();

		var storeOptions = options ?? new InMemoryEventStoryOptions(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1), new PeriodicObservable.RetryForeverPollStrategy<Event, long>(e => e.Position));
		services.AddSingleton<WakeUp>(provider => new(storeOptions.MinPollInterval, storeOptions.MaxPollInterval, provider.GetRequiredService<ILogger<Event>>()));
		
		services.AddEventSourcing(new EventStoreServices(), storeOptions);

		return services;
	}

	class EventStoreServices : IEventStoreServiceRegistration<InMemoryEventStoryOptions>
	{
		public EventStream<Event> BuildEventStream(IServiceProvider provider, InMemoryEventStoryOptions options)
		{
			var inMemoryEventStore = provider.GetRequiredService<InMemoryEventStore>();
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

		public void AddEventReader(IServiceCollection services, InMemoryEventStoryOptions options)
		{
			services.AddTransient<IEventReader>(provider => provider.GetRequiredService<InMemoryEventStore>());
		}

		public void AddEventWriter(IServiceCollection services, InMemoryEventStoryOptions options)
		{
			services.AddTransient<IEventWriter>(provider => provider.GetRequiredService<InMemoryEventStore>());
		}

		public void AddEventSerializer(IServiceCollection services, InMemoryEventStoryOptions options)
		{
		}
	}
}