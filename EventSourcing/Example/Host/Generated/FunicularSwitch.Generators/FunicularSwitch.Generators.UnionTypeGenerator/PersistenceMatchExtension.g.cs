using System;
using System.Threading.Tasks;

namespace Example.Host
{
	public static partial class MatchExtension
	{
		public static T Match<T>(this Example.Host.Persistence persistence, Func<Example.Host.Persistence.InMemoryNoMappers_, T> inMemoryNoMappers, Func<Example.Host.Persistence.SqlStreamStore_, T> sqlStreamStore) =>
		persistence switch
		{
			Example.Host.Persistence.InMemoryNoMappers_ case1 => inMemoryNoMappers(case1),
			Example.Host.Persistence.SqlStreamStore_ case2 => sqlStreamStore(case2),
			_ => throw new ArgumentException($"Unknown type derived from Example.Host.Persistence: {persistence.GetType().Name}")
		};
		
		public static Task<T> Match<T>(this Example.Host.Persistence persistence, Func<Example.Host.Persistence.InMemoryNoMappers_, Task<T>> inMemoryNoMappers, Func<Example.Host.Persistence.SqlStreamStore_, Task<T>> sqlStreamStore) =>
		persistence switch
		{
			Example.Host.Persistence.InMemoryNoMappers_ case1 => inMemoryNoMappers(case1),
			Example.Host.Persistence.SqlStreamStore_ case2 => sqlStreamStore(case2),
			_ => throw new ArgumentException($"Unknown type derived from Example.Host.Persistence: {persistence.GetType().Name}")
		};
		
		public static async Task<T> Match<T>(this Task<Example.Host.Persistence> persistence, Func<Example.Host.Persistence.InMemoryNoMappers_, T> inMemoryNoMappers, Func<Example.Host.Persistence.SqlStreamStore_, T> sqlStreamStore) =>
		(await persistence.ConfigureAwait(false)).Match(inMemoryNoMappers, sqlStreamStore);
		
		public static async Task<T> Match<T>(this Task<Example.Host.Persistence> persistence, Func<Example.Host.Persistence.InMemoryNoMappers_, Task<T>> inMemoryNoMappers, Func<Example.Host.Persistence.SqlStreamStore_, Task<T>> sqlStreamStore) =>
		await (await persistence.ConfigureAwait(false)).Match(inMemoryNoMappers, sqlStreamStore).ConfigureAwait(false);
		
		public static void Switch(this Example.Host.Persistence persistence, Action<Example.Host.Persistence.InMemoryNoMappers_> inMemoryNoMappers, Action<Example.Host.Persistence.SqlStreamStore_> sqlStreamStore)
		{
			switch (persistence)
			{
				case Example.Host.Persistence.InMemoryNoMappers_ case1:
					inMemoryNoMappers(case1);
					break;
				case Example.Host.Persistence.SqlStreamStore_ case2:
					sqlStreamStore(case2);
					break;
				default:
					throw new ArgumentException($"Unknown type derived from Example.Host.Persistence: {persistence.GetType().Name}");
			}
		}
		
		public static async Task Switch(this Example.Host.Persistence persistence, Func<Example.Host.Persistence.InMemoryNoMappers_, Task> inMemoryNoMappers, Func<Example.Host.Persistence.SqlStreamStore_, Task> sqlStreamStore)
		{
			switch (persistence)
			{
				case Example.Host.Persistence.InMemoryNoMappers_ case1:
					await inMemoryNoMappers(case1).ConfigureAwait(false);
					break;
				case Example.Host.Persistence.SqlStreamStore_ case2:
					await sqlStreamStore(case2).ConfigureAwait(false);
					break;
				default:
					throw new ArgumentException($"Unknown type derived from Example.Host.Persistence: {persistence.GetType().Name}");
			}
		}
		
		public static async Task Switch(this Task<Example.Host.Persistence> persistence, Action<Example.Host.Persistence.InMemoryNoMappers_> inMemoryNoMappers, Action<Example.Host.Persistence.SqlStreamStore_> sqlStreamStore) =>
		(await persistence.ConfigureAwait(false)).Switch(inMemoryNoMappers, sqlStreamStore);
		
		public static async Task Switch(this Task<Example.Host.Persistence> persistence, Func<Example.Host.Persistence.InMemoryNoMappers_, Task> inMemoryNoMappers, Func<Example.Host.Persistence.SqlStreamStore_, Task> sqlStreamStore) =>
		await (await persistence.ConfigureAwait(false)).Switch(inMemoryNoMappers, sqlStreamStore).ConfigureAwait(false);
	}
}
