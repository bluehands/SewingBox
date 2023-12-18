#pragma warning disable 1591
using System;
using System.Threading.Tasks;

namespace EventSourcing
{
	public static partial class ReadFailureMatchExtension
	{
		public static T Match<T>(this EventSourcing.ReadFailure readFailure, Func<EventSourcing.ReadFailure.Temporary_, T> temporary, Func<EventSourcing.ReadFailure.Permanent_, T> permanent) =>
		readFailure switch
		{
			EventSourcing.ReadFailure.Temporary_ case1 => temporary(case1),
			EventSourcing.ReadFailure.Permanent_ case2 => permanent(case2),
			_ => throw new ArgumentException($"Unknown type derived from EventSourcing.ReadFailure: {readFailure.GetType().Name}")
		};
		
		public static Task<T> Match<T>(this EventSourcing.ReadFailure readFailure, Func<EventSourcing.ReadFailure.Temporary_, Task<T>> temporary, Func<EventSourcing.ReadFailure.Permanent_, Task<T>> permanent) =>
		readFailure switch
		{
			EventSourcing.ReadFailure.Temporary_ case1 => temporary(case1),
			EventSourcing.ReadFailure.Permanent_ case2 => permanent(case2),
			_ => throw new ArgumentException($"Unknown type derived from EventSourcing.ReadFailure: {readFailure.GetType().Name}")
		};
		
		public static async Task<T> Match<T>(this Task<EventSourcing.ReadFailure> readFailure, Func<EventSourcing.ReadFailure.Temporary_, T> temporary, Func<EventSourcing.ReadFailure.Permanent_, T> permanent) =>
		(await readFailure.ConfigureAwait(false)).Match(temporary, permanent);
		
		public static async Task<T> Match<T>(this Task<EventSourcing.ReadFailure> readFailure, Func<EventSourcing.ReadFailure.Temporary_, Task<T>> temporary, Func<EventSourcing.ReadFailure.Permanent_, Task<T>> permanent) =>
		await (await readFailure.ConfigureAwait(false)).Match(temporary, permanent).ConfigureAwait(false);
		
		public static void Switch(this EventSourcing.ReadFailure readFailure, Action<EventSourcing.ReadFailure.Temporary_> temporary, Action<EventSourcing.ReadFailure.Permanent_> permanent)
		{
			switch (readFailure)
			{
				case EventSourcing.ReadFailure.Temporary_ case1:
					temporary(case1);
					break;
				case EventSourcing.ReadFailure.Permanent_ case2:
					permanent(case2);
					break;
				default:
					throw new ArgumentException($"Unknown type derived from EventSourcing.ReadFailure: {readFailure.GetType().Name}");
			}
		}
		
		public static async Task Switch(this EventSourcing.ReadFailure readFailure, Func<EventSourcing.ReadFailure.Temporary_, Task> temporary, Func<EventSourcing.ReadFailure.Permanent_, Task> permanent)
		{
			switch (readFailure)
			{
				case EventSourcing.ReadFailure.Temporary_ case1:
					await temporary(case1).ConfigureAwait(false);
					break;
				case EventSourcing.ReadFailure.Permanent_ case2:
					await permanent(case2).ConfigureAwait(false);
					break;
				default:
					throw new ArgumentException($"Unknown type derived from EventSourcing.ReadFailure: {readFailure.GetType().Name}");
			}
		}
		
		public static async Task Switch(this Task<EventSourcing.ReadFailure> readFailure, Action<EventSourcing.ReadFailure.Temporary_> temporary, Action<EventSourcing.ReadFailure.Permanent_> permanent) =>
		(await readFailure.ConfigureAwait(false)).Switch(temporary, permanent);
		
		public static async Task Switch(this Task<EventSourcing.ReadFailure> readFailure, Func<EventSourcing.ReadFailure.Temporary_, Task> temporary, Func<EventSourcing.ReadFailure.Permanent_, Task> permanent) =>
		await (await readFailure.ConfigureAwait(false)).Switch(temporary, permanent).ConfigureAwait(false);
	}
}
#pragma warning restore 1591
