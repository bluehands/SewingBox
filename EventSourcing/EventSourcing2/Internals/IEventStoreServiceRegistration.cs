using System;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing2.Internals;

public interface IEventStoreServiceRegistration<in TOptions>
{
    public void AddEventStream(IServiceCollection services, TOptions options);
    /// <summary>
    /// Register IEventReader implementation.
    /// </summary>
    public void AddEventReader(IServiceCollection services, TOptions options);
    /// <summary>
    /// Register IEventWriter implementation.
    /// </summary>
    public void AddEventWriter(IServiceCollection services, TOptions options);
    /// <summary>
    /// Register IEventSerializer implementation.
    /// </summary>
    public void AddEventSerializer(IServiceCollection services, TOptions options);
    /// <summary>
    /// Register IDbEventDescriptor implementation.
    /// </summary>
    public void AddDbEventDescriptor(IServiceCollection services);
}