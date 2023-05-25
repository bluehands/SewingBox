using System.Collections.Concurrent;
using System.Collections.Immutable;
using Aspects.Cache;
using Microsoft.Extensions.Caching.Memory;
using Nito.AsyncEx;

namespace DoubleCheckedLock
{
	public interface ICache
	{
		Task<string> GetItem(int key);
	}

	public class DoubleCheckedLockCache : ICache
	{
		ImmutableDictionary<int, string> _items = ImmutableDictionary<int, string>.Empty;

		readonly SemaphoreSlim _lock = new(1);
		readonly Database _database = new();

		public async Task<string> GetItem(int key)
		{
			if (_items.TryGetValue(key, out var item)) { return item; }

			await _lock.WaitAsync();
			try
			{
				if (_items.TryGetValue(key, out item)) { return item; }

				item = await _database.Load(key);
				_items = _items.SetItem(key, item);
				return item;
			}
			finally
			{
				_lock.Release();
			}
		}
	}

	public class LockCache : ICache
	{
		ImmutableDictionary<int, string> _items = ImmutableDictionary<int, string>.Empty;

		readonly SemaphoreSlim _lock = new(1);
		readonly Database _database = new();

		public async Task<string> GetItem(int key)
		{
			await _lock.WaitAsync();
			try
			{
				if (_items.TryGetValue(key, out var item)) { return item; }

				item = await _database.Load(key);
				_items = _items.SetItem(key, item);
				return item;
			}
			finally
			{
				_lock.Release();
			}
		}
	}

	public class MicrosoftMemoryCache : ICache
	{
		readonly MemoryCache _items = new(new MemoryCacheOptions()
		{
			ExpirationScanFrequency = TimeSpan.MaxValue
		});

		readonly Database _database = new();

		public async Task<string> GetItem(int key)
		{
			return (await _items.GetOrCreateAsync(key, cacheEntry =>
			{
				cacheEntry.AbsoluteExpiration = DateTimeOffset.MaxValue;
				return _database.Load(key);
			}))!;
		}
	}

	public class ConcurrentDictCache : ICache
	{
		readonly ConcurrentDictionary<int, AsyncLazy<string>> _items = new();

		readonly Database _database = new();

		public async Task<string> GetItem(int key) =>
			await _items.GetOrAdd(key, _ =>
			{
				return new(() => _database.Load(key));
			});
	}

	public class AspectCache : ICache
	{
		readonly Database _database = new();

		[MemoryCache(3, PerInstanceCache = true)]
		public Task<string> GetItem(int key) => _database.Load(key);
	}
}