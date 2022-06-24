using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ObservableExplorations.Infrastructure;

namespace ObservableExplorations;

// introduction to Observable.Create
//	- sync
//	- async
//  - one 'pipeline' per subscriber is default -> publish
// from callback
// from callback with replay
// polling
//errors in OnNext and operators


[TestClass]
public class Observables
{
	[TestMethod]
	public async Task Creation()
	{
		var myObservable = Observable.Create<string>(async (observer, cancellationToken) =>
		{
			observer.OnNextAndLog("oins");
			observer.OnNextAndLog("zwoi");
			await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
			observer.OnNextAndLog("droi");

			return Disposable.Create(() => Logger.Log("All observers unsubscribed"));
		});

		var subscription = myObservable
			.Subscribe(i => Logger.Log($"1 Received {i}"));

		await Task.Delay(TimeSpan.FromSeconds(1));
		subscription.Dispose();
	}

	[TestMethod]
	public async Task Errors()
	{
		var myObservable = Observable.Create<string>(async (observer, cancellationToken) =>
		{
			observer.OnNextAndLog("oins");
			observer.OnNextAndLog("zwoi");
			await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
			observer.OnNextAndLog("droi");

			return Disposable.Create(() => Logger.Log("All observers unsubscribed"));
		})
				.Publish()
			//.Where(s => s == "zwoi" ? throw new("Boom") : true)
			;

		var subscription1 = myObservable
			.Subscribe(i =>
			{
				Logger.Log($"1 Received {i}");
				
			}, onError: ex => Logger.Log($"Upstream failure: {ex}"));

		var subscription2 = myObservable
			.Subscribe(new MyObserver()
				//Logger.Log($"2 Received {i}");
				//if (i == "zwoi")
				//	throw new("BOom");
			);

		myObservable.Connect();

		await Task.Delay(TimeSpan.FromSeconds(1));
	}

	[TestMethod]
	public async Task FromCallback()
	{
		var broker = new MyMessageBroker();

		var messages = Observable.Create<string>(observer =>
		{
			var token = broker.Subscribe(message =>
			{
				observer.OnNext(message);
			});
			return Disposable.Create(() =>
			{
				broker.Unsubscribe(token);
			});
		});

		var subscription = messages
			.Subscribe(i => Logger.Log($"1 Received {i}"));

		broker.SendMessage("Hallo");
		broker.SendMessage("you there");

		var subscription2 = messages
			.Subscribe(i => Logger.Log($"2 Received {i}"));

		subscription.Dispose();
		subscription2.Dispose();

		await Task.Delay(TimeSpan.FromSeconds(1));
	}

	[TestMethod]
	public async Task FromPolling()
	{
		var eventStore = new EventStore();

		var events = Observable.Create<EventStore.Event>(observer => Scheduler.Default.ScheduleAsync(
			async (scheduler, cancellationToken) =>
		{
			long lastEventVersion = 0;
			while (!cancellationToken.IsCancellationRequested)
			{
				var events = await eventStore.GetNewerEvents(lastEventVersion).ConfigureAwait(false);
				foreach (var @event in events)
				{
					observer.OnNext(@event);
				}

				if (events.Count > 0)
					lastEventVersion = events[^1].Version;
				await scheduler.Sleep(TimeSpan.FromMilliseconds(100), cancellationToken);
			}
		})).Publish()
			.RefCount();

		events.Subscribe(e => Logger.Log($"Event {e.Version} received: {e.Payload}"));

		await Task.Run(async () =>
		{
			await eventStore.Add(new[] { "Vogel gesichtet" });
			await Task.Delay(140);
			await eventStore.Add(new[] { "Vogel wieder weg" });
			await eventStore.Add(new[] { "Alle Vögel waren schon da" });
		});

		await Task.Delay(TimeSpan.FromSeconds(1));
	}
}

public class MyObserver : IObserver<string>
{
	public void OnCompleted()
	{
	}

	public void OnError(Exception error)
	{
	}

	public void OnNext(string value)
	{
	}
}

static class Logger
{
	public static void Log(string message) => Console.WriteLine($"{DateTime.Now:HH:mm:ss fff} ({Thread.CurrentThread.ManagedThreadId:D2}) - {message}");
}

