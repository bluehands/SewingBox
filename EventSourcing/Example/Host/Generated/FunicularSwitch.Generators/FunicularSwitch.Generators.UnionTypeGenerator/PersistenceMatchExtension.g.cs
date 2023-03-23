using System;
using System.Threading.Tasks;

namespace Example.Host
{
	public static partial class MatchExtension
	{
		public static T Match<T>(this Example.Host.Persistence persistence, Func<Example.Host.Persistence.InMemoryNoMappers_, T> inMemoryNoMappers, Func<Example.Host.Persistence.SqlStreamStore_, T> sqlStreamStore, Func<Example.Host.Persistence.SQLite_, T> sQLite) =>
		persistence switch
		{
			Example.Host.Persistence.InMemoryNoMappers_ case1 => inMemoryNoMappers(case1),
			Example.Host.Persistence.SqlStreamStore_ case2 => sqlStreamStore(case2),
			Example.Host.Persistence.SQLite_ case3 => sQLite(case3),
			_ => throw new ArgumentException($"Unknown type derived from Example.Host.Persistence: {persistence.GetType().Name}")
		};
		
		public static Task<T> Match<T>(this Example.Host.Persistence persistence, Func<Example.Host.Persistence.InMemoryNoMappers_, Task<T>> inMemoryNoMappers, Func<Example.Host.Persistence.SqlStreamStore_, Task<T>> sqlStreamStore, Func<Example.Host.Persistence.SQLite_, Task<T>> sQLite) =>
		persistence switch
		{
			Example.Host.Persistence.InMemoryNoMappers_ case1 => inMemoryNoMappers(case1),
			Example.Host.Persistence.SqlStreamStore_ case2 => sqlStreamStore(case2),
			Example.Host.Persistence.SQLite_ case3 => sQLite(case3),
			_ => throw new ArgumentException($"Unknown type derived from Example.Host.Persistence: {persistence.GetType().Name}")
		};
		
		public static async Task<T> Match<T>(this Task<Example.Host.Persistence> persistence, Func<Example.Host.Persistence.InMemoryNoMappers_, T> inMemoryNoMappers, Func<Example.Host.Persistence.SqlStreamStore_, T> sqlStreamStore, Func<Example.Host.Persistence.SQLite_, T> sQLite) =>
		(await persistence.ConfigureAwait(false)).Match(inMemoryNoMappers, sqlStreamStore, sQLite);
		
		public static async Task<T> Match<T>(this Task<Example.Host.Persistence> persistence, Func<Example.Host.Persistence.InMemoryNoMappers_, Task<T>> inMemoryNoMappers, Func<Example.Host.Persistence.SqlStreamStore_, Task<T>> sqlStreamStore, Func<Example.Host.Persistence.SQLite_, Task<T>> sQLite) =>
		await (await persistence.ConfigureAwait(false)).Match(inMemoryNoMappers, sqlStreamStore, sQLite).ConfigureAwait(false);
		
		public static void Switch(this Example.Host.Persistence persistence, Action<Example.Host.Persistence.InMemoryNoMappers_> inMemoryNoMappers, Action<Example.Host.Persistence.SqlStreamStore_> sqlStreamStore, Action<Example.Host.Persistence.SQLite_> sQLite)
		{
			switch (persistence)
			{
				case Example.Host.Persistence.InMemoryNoMappers_ case1:
					inMemoryNoMappers(case1);
					break;
				case Example.Host.Persistence.SqlStreamStore_ case2:
					sqlStreamStore(case2);
					break;
				case Example.Host.Persistence.SQLite_ case3:
					sQLite(case3);
					break;
				default:
					throw new ArgumentException($"Unknown type derived from Example.Host.Persistence: {persistence.GetType().Name}");
			}
		}
		
		public static async Task Switch(this Example.Host.Persistence persistence, Func<Example.Host.Persistence.InMemoryNoMappers_, Task> inMemoryNoMappers, Func<Example.Host.Persistence.SqlStreamStore_, Task> sqlStreamStore, Func<Example.Host.Persistence.SQLite_, Task> sQLite)
		{
			switch (persistence)
			{
				case Example.Host.Persistence.InMemoryNoMappers_ case1:
					await inMemoryNoMappers(case1).ConfigureAwait(false);
					break;
				case Example.Host.Persistence.SqlStreamStore_ case2:
					await sqlStreamStore(case2).ConfigureAwait(false);
					break;
				case Example.Host.Persistence.SQLite_ case3:
					await sQLite(case3).ConfigureAwait(false);
					break;
				default:
					throw new ArgumentException($"Unknown type derived from Example.Host.Persistence: {persistence.GetType().Name}");
			}
		}
		
		public static async Task Switch(this Task<Example.Host.Persistence> persistence, Action<Example.Host.Persistence.InMemoryNoMappers_> inMemoryNoMappers, Action<Example.Host.Persistence.SqlStreamStore_> sqlStreamStore, Action<Example.Host.Persistence.SQLite_> sQLite) =>
		(await persistence.ConfigureAwait(false)).Switch(inMemoryNoMappers, sqlStreamStore, sQLite);
		
		public static async Task Switch(this Task<Example.Host.Persistence> persistence, Func<Example.Host.Persistence.InMemoryNoMappers_, Task> inMemoryNoMappers, Func<Example.Host.Persistence.SqlStreamStore_, Task> sqlStreamStore, Func<Example.Host.Persistence.SQLite_, Task> sQLite) =>
		await (await persistence.ConfigureAwait(false)).Switch(inMemoryNoMappers, sqlStreamStore, sQLite).ConfigureAwait(false);
	}
}
