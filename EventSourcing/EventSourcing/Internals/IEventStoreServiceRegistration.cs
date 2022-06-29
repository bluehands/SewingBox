using EventSourcing.Events;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing.Internals;

public interface IEventStoreServiceRegistration<in TOptions>
{
	public EventStream<Event> BuildEventStream(IServiceProvider provider, TOptions options);
	public void AddEventReader(IServiceCollection services, TOptions options);
	public void AddEventWriter(IServiceCollection services, TOptions options);
	public void AddEventSerializer(IServiceCollection services, TOptions options);
}