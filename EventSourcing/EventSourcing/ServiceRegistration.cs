using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace EventSourcing;

public static class ServiceCollectionExtension
{
	public static IServiceCollection RegisterEventPayloads(this IServiceCollection serviceCollection, IEnumerable<Assembly> payloadAssemblies)
	{
		 EventFactory.Initialize(payloadAssemblies);
		 return serviceCollection;
	}

	public static IServiceCollection RegisterPayloadMappers(this IServiceCollection serviceCollection, IEnumerable<Assembly> payloadMapperAssemblies)
	{
		EventPayloadMapper.Register(payloadMapperAssemblies);
		return serviceCollection;
	}
}