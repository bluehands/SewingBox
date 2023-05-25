using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Enumerable = System.Linq.Enumerable;

namespace DoubleCheckedLock;

public class Program
{
	static void Main(string[] args)
	{
		//for (int i = 0; i < 100; i++)
		//{
		//	new Benchmark().AspectCache().GetAwaiter().GetResult();	
		//}

		var summary = BenchmarkRunner.Run<CacheBenchmark>();

		Console.WriteLine(summary);
	}

	[InProcess()]
	[MemoryDiagnoser]
	public class CacheBenchmark
	{
		readonly CacheAccessor _accessor;

		public CacheBenchmark() => _accessor = new(
			numberOfQueriedValues: 1000,
			degreeOfParallelism: 1000,
			accessCountPerTask: 1000
		);

		[Benchmark]
		public async Task Baseline() => await _accessor.Baseline();

		[Benchmark]
		public Task Lock() => _accessor.UseCache<LockCache>();

		[Benchmark]
		public Task DoubleChecked() => _accessor.UseCache<DoubleCheckedLockCache>();

		[Benchmark]
		public Task MicrosoftMemCache() => _accessor.UseCache<MicrosoftMemoryCache>();

		[Benchmark]
		public Task ConcurrentDictCache() => _accessor.UseCache<ConcurrentDictCache>();

		[Benchmark]
		public Task AspectCache() => _accessor.UseCache<AspectCache>();
	}

	[InProcess()]
	[MemoryDiagnoser]
	public class EnumBenchmark
	{
		[Benchmark()]
		public void ToStringSwitchExpression() => MyEnum.Three.ToStringSwitchExpression();

		[Benchmark()]
		public void ToStringMatch() => MyEnum.Three.ToStringMatch();
	}

		
	public class CacheAccessor
	{
		readonly int _degreeOfParallelism;
		readonly int _numberOfQueriedValues;
		readonly int _accessCountPerTask;

		public CacheAccessor(int numberOfQueriedValues, int degreeOfParallelism, int accessCountPerTask)
		{
			_degreeOfParallelism = degreeOfParallelism;
			_numberOfQueriedValues = numberOfQueriedValues;
			_accessCountPerTask = accessCountPerTask;
		}

		public async Task Baseline()
		{
			var random = new Random();

			var tasks = Enumerable.Range(0, _degreeOfParallelism)
				.Select(_ =>
				{
					return Task.Run(async () =>
					{
						await Task.Yield();
						for (var j = 0; j < _accessCountPerTask; j++)
						{
							random.Next(_numberOfQueriedValues);
						}
					});
				});

			await Task.WhenAll(tasks);
		}

		public async Task UseCache<T>() where T : ICache, new()
		{
			var random = new Random();
			var cache = new T();

			var tasks = Enumerable.Range(0, _degreeOfParallelism)
				.Select(_ =>
				{
					return Task.Run(async () =>
					{
						await Task.Yield();
						for (var j = 0; j < _accessCountPerTask; j++)
						{
							await cache.GetItem(random.Next(_numberOfQueriedValues));
						}
					});
				});

			await Task.WhenAll(tasks);
		}
	}

	
}