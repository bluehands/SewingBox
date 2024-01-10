using EventSourcing2;
using EventSourcing2.Internals;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventSourcing.Persistence.EntityFramework;

public class EventStoreOptions : EventSourcing2.EventStoreOptions
{
    public TimeSpan MinPollWaitTime { get; init; } = TimeSpan.Zero;
    public TimeSpan MaxPollWaitTime { get; init; } = TimeSpan.FromSeconds(1);
}

public static class ServiceRegistration
{
    public static IServiceCollection AddEntityFrameworkEventStore(this IServiceCollection services, EventStoreOptions options, Action<DbContextOptionsBuilder> configureDbContext, IEventStoreServiceRegistration<EventStoreOptions>? storeServiceRegistration = null)
    {
        services.AddDbContext<EventStoreContext>(configureDbContext);
        return services.AddEventSourcing<EventStoreOptions, Event, string>(storeServiceRegistration ?? new DefaultStoreRegistration(), options);
    }

    public static async Task StartEventSourcing(this IServiceProvider serviceProvider, bool databaseMigration = true)
    {
        if (databaseMigration)
        {
            using var scope = serviceProvider.CreateScope();
            await scope.ServiceProvider.GetRequiredService<EventStoreContext>().Database.MigrateAsync();
        }
        serviceProvider.GetRequiredService<EventStream<EventSourcing2.Event>>().Start();
    }

    public class DefaultStoreRegistration : IEventStoreServiceRegistration<EventStoreOptions>
    {
        public virtual void AddEventStream(IServiceCollection services, EventStoreOptions options)
        {
            services.AddSingleton(sp => new WakeUp(options.MinPollWaitTime, options.MaxPollWaitTime, sp.GetService<ILogger<WakeUp>>()));
            services.AddSingleton(sp => BuildPollingEventStream(sp, options));
        }

        static EventStream<EventSourcing2.Event> BuildPollingEventStream(IServiceProvider provider, EventStoreOptions options)
        {
            var streamScope = provider.CreateScope();

            var eventReader = streamScope.ServiceProvider.GetRequiredService<IEventStore>();
            var wakeUp = provider.GetRequiredService<WakeUp>();

            var events = PeriodicObservable.Poll(
                () => Task.FromResult(0L),
                eventReader.ReadEvents,
                wakeUp,
                provider.GetService<ILogger<EventStream<EventSourcing2.Event>>>()
            );

            return new(events, streamScope);
        }

        public virtual void AddEventReader(IServiceCollection services, EventStoreOptions options)
        {
            services.AddTransient<IEventReader<Event>, EventStore>();
        }

        public virtual void AddEventWriter(IServiceCollection services, EventStoreOptions options)
        {
            services.AddTransient<IEventWriter<Event>, EventStore>();
        }

        public virtual void AddEventSerializer(IServiceCollection services, EventStoreOptions options)
        {
            services.AddTransient<IEventSerializer<string>, JsonEventSerializer>();
        }

        public virtual void AddDbEventDescriptor(IServiceCollection services)
        {
            services.AddTransient<IDbEventDescriptor<Event, string>, EventDescriptor>();
        }
    }
}

class EventDescriptor : IDbEventDescriptor<Event, string>
{
    public string GetEventType(Event dbEvent) => dbEvent.EventType;

    public string GetPayload(Event dbEvent) => dbEvent.Payload;

    public long GetPosition(Event dbEvent) => dbEvent.Position;

    public DateTimeOffset GetTimestamp(Event dbEvent) => dbEvent.Timestamp;

    public Event CreateDbEvent(StreamId streamId, string eventType, string payload) => new(0, streamId.StreamType, streamId.Id, eventType, payload, DateTimeOffset.Now);
}

class JsonEventSerializer : IEventSerializer<string>
{
    public string Serialize(object serializablePayload) => System.Text.Json.JsonSerializer.Serialize(serializablePayload);

    public object Deserialize(Type serializablePayloadType, string serializedPayload) => System.Text.Json.JsonSerializer.Deserialize(serializedPayload, serializablePayloadType)!;
}