using EventSourcing;
using Example.Host;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Example.Test;

public class TestHost
{
	public IServiceProvider Services { get; }

	public TestHost()
	{
		var serviceCollection = new ServiceCollection();
		serviceCollection
			.AddExampleApp(Persistence.SqlStreamStore(StreamStoreDemoOptions.InMemory))
			.AddLogging(builder => builder
				.AddConsole()
				.SetMinimumLevel(LogLevel.Trace)
			);

		Services = serviceCollection.BuildServiceProvider();
		Services.UseEventSourcing();
	}

	public static readonly TestHost Instance = new();
}