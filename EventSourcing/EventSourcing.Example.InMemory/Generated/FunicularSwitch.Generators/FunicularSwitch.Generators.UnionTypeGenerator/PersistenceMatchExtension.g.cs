using System;
using System.Threading.Tasks;

public static partial class MatchExtension
{
	public static T Match<T>(this Persistence persistence, Func<Persistence.InMemoryNoMappers_, T> inMemoryNoMappers, Func<Persistence.SqlStreamStore_, T> sqlStreamStore) =>
	persistence switch
	{
		Persistence.InMemoryNoMappers_ case1 => inMemoryNoMappers(case1),
		Persistence.SqlStreamStore_ case2 => sqlStreamStore(case2),
		_ => throw new ArgumentException($"Unknown type derived from Persistence: {persistence.GetType().Name}")
	};
	
	public static Task<T> Match<T>(this Persistence persistence, Func<Persistence.InMemoryNoMappers_, Task<T>> inMemoryNoMappers, Func<Persistence.SqlStreamStore_, Task<T>> sqlStreamStore) =>
	persistence switch
	{
		Persistence.InMemoryNoMappers_ case1 => inMemoryNoMappers(case1),
		Persistence.SqlStreamStore_ case2 => sqlStreamStore(case2),
		_ => throw new ArgumentException($"Unknown type derived from Persistence: {persistence.GetType().Name}")
	};
	
	public static async Task<T> Match<T>(this Task<Persistence> persistence, Func<Persistence.InMemoryNoMappers_, T> inMemoryNoMappers, Func<Persistence.SqlStreamStore_, T> sqlStreamStore) =>
	(await persistence.ConfigureAwait(false)).Match(inMemoryNoMappers, sqlStreamStore);
	
	public static async Task<T> Match<T>(this Task<Persistence> persistence, Func<Persistence.InMemoryNoMappers_, Task<T>> inMemoryNoMappers, Func<Persistence.SqlStreamStore_, Task<T>> sqlStreamStore) =>
	await (await persistence.ConfigureAwait(false)).Match(inMemoryNoMappers, sqlStreamStore).ConfigureAwait(false);
	
	public static void Switch(this Persistence persistence, Action<Persistence.InMemoryNoMappers_> inMemoryNoMappers, Action<Persistence.SqlStreamStore_> sqlStreamStore)
	{
		switch (persistence)
		{
			case Persistence.InMemoryNoMappers_ case1:
				inMemoryNoMappers(case1);
				break;
			case Persistence.SqlStreamStore_ case2:
				sqlStreamStore(case2);
				break;
			default:
				throw new ArgumentException($"Unknown type derived from Persistence: {persistence.GetType().Name}");
		}
	}
	
	public static async Task Switch(this Persistence persistence, Func<Persistence.InMemoryNoMappers_, Task> inMemoryNoMappers, Func<Persistence.SqlStreamStore_, Task> sqlStreamStore)
	{
		switch (persistence)
		{
			case Persistence.InMemoryNoMappers_ case1:
				await inMemoryNoMappers(case1).ConfigureAwait(false);
				break;
			case Persistence.SqlStreamStore_ case2:
				await sqlStreamStore(case2).ConfigureAwait(false);
				break;
			default:
				throw new ArgumentException($"Unknown type derived from Persistence: {persistence.GetType().Name}");
		}
	}
	
	public static async Task Switch(this Task<Persistence> persistence, Action<Persistence.InMemoryNoMappers_> inMemoryNoMappers, Action<Persistence.SqlStreamStore_> sqlStreamStore) =>
	(await persistence.ConfigureAwait(false)).Switch(inMemoryNoMappers, sqlStreamStore);
	
	public static async Task Switch(this Task<Persistence> persistence, Func<Persistence.InMemoryNoMappers_, Task> inMemoryNoMappers, Func<Persistence.SqlStreamStore_, Task> sqlStreamStore) =>
	await (await persistence.ConfigureAwait(false)).Switch(inMemoryNoMappers, sqlStreamStore).ConfigureAwait(false);
}
