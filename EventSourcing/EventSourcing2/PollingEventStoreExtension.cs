using System;
using System.Threading.Tasks;
using EventSourcing2.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventSourcing2;

public interface IAllowPollingEventStreamBuilder;

public static class PollingEventStoreExtension
{
    public static TBuilder UsePollingEventStream<TBuilder>(this TBuilder builder, TimeSpan minWaitTime, TimeSpan maxWaitTime, Func<Task<long>> getPositionToStartFrom) 
        where TBuilder : IAllowPollingEventStreamBuilder, IEventSourcingExtensionsBuilderInfrastructure =>
        builder.WithOption<TBuilder, PollingEventStreamOptionsExtension>(e => e with
        {
            MinWaitTime = minWaitTime,
            MaxWaitTime = maxWaitTime,
            GetPositionToStartFrom = getPositionToStartFrom
        });

    public static TBuilder UsePollingEventStream<TBuilder>(this TBuilder builder) 
        where TBuilder : IAllowPollingEventStreamBuilder, IEventSourcingExtensionsBuilderInfrastructure =>
        builder.WithOption<TBuilder, PollingEventStreamOptionsExtension>(e => e);
}

public record PollingEventStreamOptionsExtension(TimeSpan? MinWaitTime, TimeSpan? MaxWaitTime, Func<Task<long>>? GetPositionToStartFrom) : IEventSourcingOptionsExtension
{
    public PollingEventStreamOptionsExtension() : this(null, null, null)
    {
    }

    public void ApplyServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton(sp => new WakeUp(MinWaitTime ?? TimeSpan.Zero, MaxWaitTime ?? TimeSpan.FromMilliseconds(100), sp.GetService<ILogger<WakeUp>>()));
        serviceCollection.AddSingleton(sp => BuildPollingEventStream(sp, GetPositionToStartFrom ?? (() => Task.FromResult(0L))));
        serviceCollection.AddSingleton<IObservable<Event>>(sp => sp.GetRequiredService<EventStream<Event>>());
    }

    static EventStream<Event> BuildPollingEventStream(IServiceProvider provider, Func<Task<long>> getPositionToStartFrom)
    {
        var streamScope = provider.CreateScope();

        var eventReader = streamScope.ServiceProvider.GetRequiredService<IEventStore>();
        var wakeUp = provider.GetRequiredService<WakeUp>();

        var events = PeriodicObservable.Poll(
            getPositionToStartFrom,
            eventReader.ReadEvents,
            wakeUp,
            provider.GetService<ILogger<EventStream<Event>>>()
        );

        return new(events, streamScope);
    }
}