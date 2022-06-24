using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ObservableExplorations.Infrastructure;

namespace ObservableExplorations
{
	[TestClass]
	public class Demo
	{
		[TestMethod]
		public async Task Creation()
		{
			Logger.Log("Hallo");
			var myObservable = Observable.Create<string>(
					async (observer, cancellationToken) =>
					{
						observer.OnNextAndLog("oins");
						observer.OnNextAndLog("zwoi");
						await Task.Delay(200);
						observer.OnNextAndLog("droi");
					}
				)
				.Select(s =>
				{
					Logger.Log($"Message received {s}");
					return $"Selected {s}";
				})
				.Publish()
				.RefCount();

			var subscription = myObservable.Subscribe(s => Logger.Log($"Sub1 Received {s}"));
			myObservable.Subscribe(s => Logger.Log($"Sub2 Received {s}"));

			await Task.Delay(TimeSpan.FromSeconds(1));
		}

		[TestMethod]
		public async Task FromCallback()
		{
			var messageBroker = new MyMessageBroker();

			var messages = Observable.Create<string>(observer =>
			{
				var token = messageBroker.Subscribe(message => observer.OnNext(message));
				return Disposable.Create(() => messageBroker.Unsubscribe(token));
			})
				.Replay(1);
			
			var connection = messages.Connect();

			var sub1 = messages.Subscribe(s => Logger.Log($"Sub1 Received message {s}"));

			messageBroker.SendMessage("Hallo");
			messageBroker.SendMessage("Hallo 2");

			var sub2 = messages.Subscribe(s => Logger.Log($"Sub2 Received message {s}"));

			sub1.Dispose();
			sub2.Dispose();
		}
	}

	static class ObservableExtension
	{
		public static void OnNextAndLog<T>(this IObserver<T> observer, T item)
		{
			Logger.Log($"Publishing: {item}");
			observer.OnNext(item);
		}
	}
}
