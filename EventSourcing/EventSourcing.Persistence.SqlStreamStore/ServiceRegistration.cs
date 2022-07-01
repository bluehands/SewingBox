using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using EventSourcing.Events;
using EventSourcing.Internals;
using FunicularSwitch.Generators;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlStreamStore;

namespace EventSourcing.Persistence.SqlStreamStore;

public record SqlStreamEventStoreOptions(Func<IServiceProvider, IStreamStore> CreateStore, Func<IServiceProvider, IEventSerializer<string>> CreateSerializer, PollingOptions PollingOptions, Func<Task<long>> GetLastProcessedEventPosition);

[UnionType(CaseOrder = CaseOrder.AsDeclared)]
public abstract class PollingOptions
{
	public static readonly PollingOptions NoPolling = new NoPolling_();

	public static PollingOptions UsePolling(PeriodicObservable.PollStrategy<Event, long> pollStrategy) => new UsePolling_(pollStrategy);

	public class NoPolling_ : PollingOptions
	{
		public NoPolling_() : base(UnionCases.NoPolling)
		{
		}
	}

	public class UsePolling_ : PollingOptions
	{
		public PeriodicObservable.PollStrategy<Event, long> PollStrategy { get; }

		public UsePolling_(PeriodicObservable.PollStrategy<Event, long> pollStrategy) : base(UnionCases.UsePolling) => PollStrategy = pollStrategy;
	}

	internal enum UnionCases
	{
		NoPolling,
		UsePolling
	}

	internal UnionCases UnionCase { get; }
	PollingOptions(UnionCases unionCase) => UnionCase = unionCase;

	public override string ToString() => Enum.GetName(typeof(UnionCases), UnionCase) ?? UnionCase.ToString();
	bool Equals(PollingOptions other) => UnionCase == other.UnionCase;

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != GetType()) return false;
		return Equals((PollingOptions)obj);
	}

	public override int GetHashCode() => (int)UnionCase;
}

public static class ServiceRegistration
{
	public static IServiceCollection AddSqlStreamEventStore(this IServiceCollection services, SqlStreamEventStoreOptions options = null)
	{
		options ??= new(
			CreateStore: _ => new InMemoryStreamStore(),
			CreateSerializer: _ => new JsonEventSerializer(),
			PollingOptions: PollingOptions.UsePolling(new PeriodicObservable.RetryNTimesPollStrategy<Event, long>(e => e.Position, 10, l => l + 1)), 
			GetLastProcessedEventPosition: () => Task.FromResult(-1L)
		);

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
					return EventStream.CreateWithPolling(
						getLastProcessedEventNr: options.GetLastProcessedEventPosition,
						getEventNr: e => e.Position,
						getOrderedNewEvents: versionExclusive => eventReader.ReadEvents(versionExclusive + 1),
						pollInterval: TimeSpan.FromMilliseconds(100),
						getEvents: e => Task.FromResult(e),
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

		public void AddEventReader(IServiceCollection services, SqlStreamEventStoreOptions options)
		{
			services.AddTransient<IEventReader, SqlStreamStoreEventReader>();
		}

		public void AddEventWriter(IServiceCollection services, SqlStreamEventStoreOptions options)
		{
			services.AddTransient<IEventWriter, StreamStoreEventWriter>();
		}

		public void AddEventSerializer(IServiceCollection services, SqlStreamEventStoreOptions options)
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