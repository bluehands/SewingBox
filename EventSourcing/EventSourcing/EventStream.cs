using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace EventSourcing;

public sealed class EventStream<T> : IDisposable, IObservable<T>
{
	readonly IConnectableObservable<T> _stream;
	IDisposable? _connection;

	public EventStream(IObservable<T> events) => _stream = events.Publish();

	public void Start()
	{
		Stop();
		_connection = _stream.Connect();
	}

	public void Stop()
	{
		_connection?.Dispose();
		_connection = null;
	}

	public void Dispose()
	{
		Stop();
	}

	public IDisposable Subscribe(IObserver<T> observer)
		=> _stream.Subscribe(observer);
}