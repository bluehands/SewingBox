using EventSourcing.Persistence.EntityFramework;
using EventSourcing.Persistence.EntityFramework.Sqlite;
using EventSourcing2;
using EventSourcing2.Events;
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
                serviceCollection.AddSqliteEventStore(@"Data Source=c:\temp\EventStore.db");
            })
            .ConfigureLogging(builder =>
            {
                builder.AddConsole();
            })
            .Build();

        await host.Services.StartEventSourcing();

        await host.StartAsync();

        await TryIt(host.Services);

        await host.WaitForShutdownAsync();
    }

    static async Task TryIt(IServiceProvider services)
    {
        var eventStream = services.GetRequiredService<IObservable<Event>>();
        eventStream.Subscribe(@event => Console.WriteLine($"Received: {@event}"));

        var eventStore = services.GetRequiredService<IEventStore>();
        await eventStore.WriteEvents(new[] { new MyFirstEvent("My first event :)") });

        Console.WriteLine("Reading all events from store...");
        await foreach(var @event in eventStore.ReadEvents(0))
        {
            Console.WriteLine(@event);
        }
    }
}


[SerializableEventPayload("MyFirstEvent")]
public record MyFirstEvent(string Name) : EventPayload(new("TestStreamType", "TestStreamId"), "MyFirstEvent");


//public record MyFirstSerializableEvent(string Name) {
//}

//public class MyFirstEventPayloadMapper : EventPayloadMapper<MyFirstSerializableEvent, MyFirstEvent>
//{
//    protected override MyFirstEvent MapFromSerializablePayload(MyFirstSerializableEvent serialized) => new MyFirstEvent(serialized.Name);

//    protected override MyFirstSerializableEvent MapToSerializablePayload(MyFirstEvent payload) => new MyFirstSerializableEvent(payload.Name);
//}