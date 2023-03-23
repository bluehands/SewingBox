#pragma warning disable 1591
using System;
using System.Threading.Tasks;

namespace Example.Host
{
	public static partial class MatchExtension
	{
		public static T Match<T>(this Example.Host.Persistence persistence, Func<Example.Host.Persistence.InMemoryNoMappers_, T> inMemoryNoMappers, Func<Example.Host.Persistence.SqlStreamStoreInMemory_, T> sqlStreamStoreInMemory, Func<Example.Host.Persistence.MsSqlStreamStore_, T> msSqlStreamStore, Func<Example.Host.Persistence.SQLite_, T> sQLite) =>
		persistence switch
		{
			Example.Host.Persistence.InMemoryNoMappers_ case1 => inMemoryNoMappers(case1),
			Example.Host.Persistence.SqlStreamStoreInMemory_ case2 => sqlStreamStoreInMemory(case2),
			Example.Host.Persistence.MsSqlStreamStore_ case3 => msSqlStreamStore(case3),
			Example.Host.Persistence.SQLite_ case4 => sQLite(case4),
			_ => throw new ArgumentException($"Unknown type derived from Example.Host.Persistence: {persistence.GetType().Name}")
		};
		
		public static Task<T> Match<T>(this Example.Host.Persistence persistence, Func<Example.Host.Persistence.InMemoryNoMappers_, Task<T>> inMemoryNoMappers, Func<Example.Host.Persistence.SqlStreamStoreInMemory_, Task<T>> sqlStreamStoreInMemory, Func<Example.Host.Persistence.MsSqlStreamStore_, Task<T>> msSqlStreamStore, Func<Example.Host.Persistence.SQLite_, Task<T>> sQLite) =>
		persistence switch
		{
			Example.Host.Persistence.InMemoryNoMappers_ case1 => inMemoryNoMappers(case1),
			Example.Host.Persistence.SqlStreamStoreInMemory_ case2 => sqlStreamStoreInMemory(case2),
			Example.Host.Persistence.MsSqlStreamStore_ case3 => msSqlStreamStore(case3),
			Example.Host.Persistence.SQLite_ case4 => sQLite(case4),
			_ => throw new ArgumentException($"Unknown type derived from Example.Host.Persistence: {persistence.GetType().Name}")
		};
		
		public static async Task<T> Match<T>(this Task<Example.Host.Persistence> persistence, Func<Example.Host.Persistence.InMemoryNoMappers_, T> inMemoryNoMappers, Func<Example.Host.Persistence.SqlStreamStoreInMemory_, T> sqlStreamStoreInMemory, Func<Example.Host.Persistence.MsSqlStreamStore_, T> msSqlStreamStore, Func<Example.Host.Persistence.SQLite_, T> sQLite) =>
		(await persistence.ConfigureAwait(false)).Match(inMemoryNoMappers, sqlStreamStoreInMemory, msSqlStreamStore, sQLite);
		
		public static async Task<T> Match<T>(this Task<Example.Host.Persistence> persistence, Func<Example.Host.Persistence.InMemoryNoMappers_, Task<T>> inMemoryNoMappers, Func<Example.Host.Persistence.SqlStreamStoreInMemory_, Task<T>> sqlStreamStoreInMemory, Func<Example.Host.Persistence.MsSqlStreamStore_, Task<T>> msSqlStreamStore, Func<Example.Host.Persistence.SQLite_, Task<T>> sQLite) =>
		await (await persistence.ConfigureAwait(false)).Match(inMemoryNoMappers, sqlStreamStoreInMemory, msSqlStreamStore, sQLite).ConfigureAwait(false);
		
		public static void Switch(this Example.Host.Persistence persistence, Action<Example.Host.Persistence.InMemoryNoMappers_> inMemoryNoMappers, Action<Example.Host.Persistence.SqlStreamStoreInMemory_> sqlStreamStoreInMemory, Action<Example.Host.Persistence.MsSqlStreamStore_> msSqlStreamStore, Action<Example.Host.Persistence.SQLite_> sQLite)
		{
			switch (persistence)
			{
				case Example.Host.Persistence.InMemoryNoMappers_ case1:
					inMemoryNoMappers(case1);
					break;
				case Example.Host.Persistence.SqlStreamStoreInMemory_ case2:
					sqlStreamStoreInMemory(case2);
					break;
				case Example.Host.Persistence.MsSqlStreamStore_ case3:
					msSqlStreamStore(case3);
					break;
				case Example.Host.Persistence.SQLite_ case4:
					sQLite(case4);
					break;
				default:
					throw new ArgumentException($"Unknown type derived from Example.Host.Persistence: {persistence.GetType().Name}");
			}
		}
		
		public static async Task Switch(this Example.Host.Persistence persistence, Func<Example.Host.Persistence.InMemoryNoMappers_, Task> inMemoryNoMappers, Func<Example.Host.Persistence.SqlStreamStoreInMemory_, Task> sqlStreamStoreInMemory, Func<Example.Host.Persistence.MsSqlStreamStore_, Task> msSqlStreamStore, Func<Example.Host.Persistence.SQLite_, Task> sQLite)
		{
			switch (persistence)
			{
				case Example.Host.Persistence.InMemoryNoMappers_ case1:
					await inMemoryNoMappers(case1).ConfigureAwait(false);
					break;
				case Example.Host.Persistence.SqlStreamStoreInMemory_ case2:
					await sqlStreamStoreInMemory(case2).ConfigureAwait(false);
					break;
				case Example.Host.Persistence.MsSqlStreamStore_ case3:
					await msSqlStreamStore(case3).ConfigureAwait(false);
					break;
				case Example.Host.Persistence.SQLite_ case4:
					await sQLite(case4).ConfigureAwait(false);
					break;
				default:
					throw new ArgumentException($"Unknown type derived from Example.Host.Persistence: {persistence.GetType().Name}");
			}
		}
		
		public static async Task Switch(this Task<Example.Host.Persistence> persistence, Action<Example.Host.Persistence.InMemoryNoMappers_> inMemoryNoMappers, Action<Example.Host.Persistence.SqlStreamStoreInMemory_> sqlStreamStoreInMemory, Action<Example.Host.Persistence.MsSqlStreamStore_> msSqlStreamStore, Action<Example.Host.Persistence.SQLite_> sQLite) =>
		(await persistence.ConfigureAwait(false)).Switch(inMemoryNoMappers, sqlStreamStoreInMemory, msSqlStreamStore, sQLite);
		
		public static async Task Switch(this Task<Example.Host.Persistence> persistence, Func<Example.Host.Persistence.InMemoryNoMappers_, Task> inMemoryNoMappers, Func<Example.Host.Persistence.SqlStreamStoreInMemory_, Task> sqlStreamStoreInMemory, Func<Example.Host.Persistence.MsSqlStreamStore_, Task> msSqlStreamStore, Func<Example.Host.Persistence.SQLite_, Task> sQLite) =>
		await (await persistence.ConfigureAwait(false)).Switch(inMemoryNoMappers, sqlStreamStoreInMemory, msSqlStreamStore, sQLite).ConfigureAwait(false);
	}
}
#pragma warning restore 1591
