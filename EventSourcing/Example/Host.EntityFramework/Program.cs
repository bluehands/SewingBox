using EventSourcing.Persistence.EntityFramework;
using EventSourcing.Persistence.EntityFramework.Sqlite;
using EventSourcing2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Event = EventSourcing2.Event;

namespace Example.Host.EntityFramework;

internal class Program
{
    static async Task Main(string[] args)
    {
        using var host = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
            .ConfigureServices(serviceCollection =>
            {
                serviceCollection.AddEntityFrameworkEventStore(new EventStoreOptions(), options =>
                {
                    options.UseSqlite(@"Data Source=c:\temp\EventStore.db", x => x.MigrationsAssembly(Provider.Sqlite.Assembly));
                });
            })
            .ConfigureLogging(builder => builder.AddConsole())
            .Build();

        using (var scope = host.Services.CreateScope())
        {
            await scope.ServiceProvider.GetRequiredService<EventStoreContext>().Database.MigrateAsync();
        }

        host.Services.GetRequiredService<EventStream<Event>>().Start();
        await host.StartAsync();
        await host.WaitForShutdownAsync();
    }

    public record Provider(string Name, string Assembly) 
    {
        public static Provider Sqlite = new (nameof(Sqlite), typeof(Marker).Assembly.GetName().Name!);
    }
}