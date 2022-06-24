namespace ObservableExplorations.Infrastructure;

class MyMessageBroker
{
	readonly Dictionary<Guid, Action<string>> _callbacks = new();

	public Guid Subscribe(Action<string> onMessageReceived)
	{
		var token = Guid.NewGuid();
		_callbacks.Add(token, onMessageReceived);
		Logger.Log($"{token} subscribed");
		return token;
	}

	public void Unsubscribe(Guid token)
	{
		_callbacks.Remove(token);
		Logger.Log($"{token} unsubscribed");
	}

	public void SendMessage(string message)
	{
		foreach (var callback in _callbacks.Values) callback(message);
	}
}