using System.Diagnostics;
using EventSourcing.Persistence.EntityFramework;
using EventSourcing.Persistence.EntityFramework.SqlServer;
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
                //serviceCollection.AddSqlServerEventStore("Data Source=.\\SQLSERVEREXPRESS;Initial Catalog=TestEventStore2;Integrated Security=True;TrustServerCertificate=True;");

                serviceCollection.AddEventSourcing(
                    b =>
                    {
                        b.UseSqlServerEventStore("Data Source=.\\SQLSERVEREXPRESS;Initial Catalog=TestEventStore2;Integrated Security=True;TrustServerCertificate=True;");
                    });

                //serviceCollection.AddSqliteEventStore(@"Data Source=c:\temp\EventStore.db");
            })
            .ConfigureLogging(builder =>
            {
                builder.AddConsole();
            })
            .Build();

        await host.StartAsync();

        await TryIt(host.Services);

        await host.WaitForShutdownAsync();
    }

    static async Task TryIt(IServiceProvider services)
    {
        //var eventStream = services.GetRequiredService<IObservable<Event>>();
        //using var subscription = eventStream.Subscribe(@event =>
        //{
        //    if (@event.Position % 1000 == 0)
        //        Console.WriteLine($"Received: {@event}");
        //});

        //await services.StartEventSourcing();

        var eventStore = services.GetRequiredService<IEventStore>();

        //Console.WriteLine("Reading all events from store...");
        //await foreach(var @event in eventStore.ReadEvents(0))
        //{
        //    if (@event.Position % 1000 == 0)
        //        Console.WriteLine($"Read from store: {@event}");
        //}

        int i = 0;
        var cancellationToken = services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping;
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
                try
                {
                    var events = Enumerable.Range(0, 10000).Select(i => 
                            new MyFirstEvent($"Batch {i}", TextGenerator.RandomString(10, 1000)))
                        .ToList();
                    var sw = Stopwatch.StartNew();
                    await eventStore.WriteEvents(events);
                    Console.WriteLine($"Wrote {events.Count} events in {sw.ElapsedMilliseconds} ms");
                    //await eventStore.WriteEvents(new[] { new MyFirstEvent($"My event {i++}") });
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to write event");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}

public static class TextGenerator
{
    static readonly Random random = new Random();

    public static string RandomString(int minLength, int maxLength) => RandomString(random.Next(minLength, maxLength));

    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}


[SerializableEventPayload("MyFirstEvent")]
public record MyFirstEvent(string Name, string Text) : EventPayload(new("TestStreamType", "TestStreamId"), "MyFirstEvent");


//public record MyFirstSerializableEvent(string Name) {
//}

//public class MyFirstEventPayloadMapper : EventPayloadMapper<MyFirstSerializableEvent, MyFirstEvent>
//{
//    protected override MyFirstEvent MapFromSerializablePayload(MyFirstSerializableEvent serialized) => new MyFirstEvent(serialized.Name);

//    protected override MyFirstSerializableEvent MapToSerializablePayload(MyFirstEvent payload) => new MyFirstSerializableEvent(payload.Name);
//}