using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reflection;
using FunicularSwitch.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing2;

public interface IEventSourcingOptionsExtension
{
    void ApplyServices(IServiceCollection serviceCollection);
}

public interface IEventSourcingOptions
{
    IEnumerable<IEventSourcingOptionsExtension> Extensions { get; }
}

public interface IEventSourcingBuilderInfrastructure
{
    void AddOrUpdateExtension<TExtension>(TExtension extension) where TExtension : class, IEventSourcingOptionsExtension;
}

public interface IEventSourcingExtensionsBuilderInfrastructure
{
    EventSourcingOptionsBuilder OptionsBuilder { get; }
}

public class EventSourcingOptions : IEventSourcingOptions
{
    readonly ImmutableDictionary<Type, IEventSourcingOptionsExtension> _extensions = ImmutableDictionary<Type, IEventSourcingOptionsExtension>.Empty;
    public IEnumerable<IEventSourcingOptionsExtension> Extensions => _extensions.Values;

    public EventSourcingOptions()
    {
    }

    public EventSourcingOptions(ImmutableDictionary<Type, IEventSourcingOptionsExtension> extensions) => _extensions = extensions;

    public TExtension? FindExtension<TExtension>() => _extensions.TryGetValue(typeof(TExtension), out var extension) ? (TExtension?)extension : default;

    public TExtension GetExtension<TExtension>()
    {
        var extension = FindExtension<TExtension>();
        return extension == null
            ? throw new InvalidOperationException($"Option extension of type {typeof(TExtension).Name} not found")
            : extension;
    }

    public EventSourcingOptions WithExtension<TExtension>(TExtension extension)
        where TExtension : IEventSourcingOptionsExtension =>
        new(_extensions.SetItem(typeof(TExtension), extension));
}

public class EventSourcingOptionsBuilder : IEventSourcingBuilderInfrastructure
{
    EventSourcingOptions _options;
    public EventSourcingOptions Options => _options;

    public static EventSourcingOptionsBuilder WithCoreOptions() => new(new EventSourcingOptions().WithExtension(new EventSourcingCoreOptionsExtension()));
    
    public EventSourcingOptionsBuilder() : this(new()) { }

    public EventSourcingOptionsBuilder(EventSourcingOptions options) => _options = options ?? throw new ArgumentNullException(nameof(options));

    public EventSourcingOptionsBuilder WithPayloadAssemblies(Assembly assembly, params Assembly[] assemblies) =>
        WithOption(e => e with
        {
            PayloadAssemblies = ImmutableList.Create<Assembly>().Add(assembly).AddRange(assemblies)
        });

    EventSourcingOptionsBuilder WithOption(Func<EventSourcingCoreOptionsExtension, EventSourcingCoreOptionsExtension> withFunc)
    {
        var optionsExtension = withFunc(_options.FindExtension<EventSourcingCoreOptionsExtension>() ?? new EventSourcingCoreOptionsExtension());
        AddOrUpdateExtension(optionsExtension);
        return this;
    }

    void AddOrUpdateExtension(EventSourcingCoreOptionsExtension optionsExtension) => ((IEventSourcingBuilderInfrastructure)this).AddOrUpdateExtension(optionsExtension);

    void IEventSourcingBuilderInfrastructure.AddOrUpdateExtension<TExtension>(TExtension extension)
    => _options = _options.WithExtension(extension);
}

public record EventSourcingCoreOptionsExtension(
    IReadOnlyCollection<Assembly>? PayloadAssemblies,
    IReadOnlyCollection<Assembly>? PayloadMapperAssemblies)
    : IEventSourcingOptionsExtension
{
    public EventSourcingCoreOptionsExtension() : this(null, null)
    {
    }

    public void ApplyServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<EventSourcingContext>();
        serviceCollection.RegisterEventPayloads(PayloadAssemblies ?? Assembly.GetEntryAssembly().Yield());
        serviceCollection.RegisterPayloadMappers(PayloadMapperAssemblies ?? Assembly.GetEntryAssembly().Yield());
    }
}