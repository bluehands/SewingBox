using System.Collections.Concurrent;

namespace DoubleCheckedLock;

class Database
{
	readonly ConcurrentBag<int> _loadedKeys = new();

	public Task<string> Load(int key)
	{
		//if (_loadedKeys.Contains(key))
		//	throw new InvalidOperationException($"Key {key} loaded multiple times");
		//_loadedKeys.Add(key);
		
		return Task.FromResult(key.ToString());
	}
}