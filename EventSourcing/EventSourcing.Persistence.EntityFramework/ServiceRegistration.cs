using EventSourcing.Persistence.EntityFramework.Internal;
using EventSourcing2;
using EventSourcing2.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventSourcing.Persistence.EntityFramework;

public static class ServiceRegistration
{
    public static IServiceCollection AddEntityFrameworkEventStore<TOptions>(this IServiceCollection services,
        TOptions options,
        Action<DbContextOptionsBuilder> configureDbContext,
        IEventStoreServiceRegistration<TOptions> storeServiceRegistration
        )
    where TOptions : EventStoreOptions
    {
        services.AddDbContext<EventStoreContext>(configureDbContext);
        return services.AddEventSourcing<TOptions, Event, string>(storeServiceRegistration, options);
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
}

public abstract class DefaultStoreRegistration<TOptions> : IEventStoreServiceRegistration<TOptions> where TOptions : EventStoreOptions
{
    protected void AddPollingEventStream(IServiceCollection services, TimeSpan minWaitTime, TimeSpan maxWaitTime, long positionToStart)
    {
        services.AddSingleton(sp => new WakeUp(minWaitTime, maxWaitTime, sp.GetService<ILogger<WakeUp>>()));
        services.AddSingleton(sp => BuildPollingEventStream(sp, positionToStart));
    }

    static EventStream<EventSourcing2.Event> BuildPollingEventStream(IServiceProvider provider, long positionToStart)
    {
        var streamScope = provider.CreateScope();

        var eventReader = streamScope.ServiceProvider.GetRequiredService<IEventStore>();
        var wakeUp = provider.GetRequiredService<WakeUp>();

        var events = PeriodicObservable.Poll(
            () => Task.FromResult(positionToStart),
            eventReader.ReadEvents,
            wakeUp,
            provider.GetService<ILogger<EventStream<EventSourcing2.Event>>>()
        );

        return new(events, streamScope);
    }

    public abstract void AddEventStream(IServiceCollection services, TOptions options);

    public virtual void AddEventReader(IServiceCollection services, TOptions options)
    {
        services.AddTransient<IEventReader<Event>, EventStore>();
    }

    public virtual void AddEventWriter(IServiceCollection services, TOptions options)
    {
        services.AddTransient<IEventWriter<Event>, EventStore>();
    }

    public virtual void AddEventSerializer(IServiceCollection services, TOptions options)
    {
        services.AddTransient<IEventSerializer<string>, JsonEventSerializer>();
    }

    public virtual void AddDbEventDescriptor(IServiceCollection services)
    {
        services.AddTransient<IDbEventDescriptor<Event, string>, EventDescriptor>();
    }
}

public static class EntityFrameworkServices
{
    public static IServiceCollection AddEntityFrameworkServices(this IServiceCollection services)
    {
        services.AddTransient<IInitializer, DatabaseInitializer>();

        services.AddTransient<IEventReader<Event>, EventStore>();
        services.AddTransient<IEventWriter<Event>, EventStore>();
        services.AddTransient<IEventSerializer<string>, JsonEventSerializer>();
        services.AddTransient<IDbEventDescriptor<Event, string>, EventDescriptor>();
        services.AddTransient<IEventStore, EventStore<Event, string>>();
        services.AddTransient<IEventMapper<Event>, EventStore<Event, string>>();
        return services;
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