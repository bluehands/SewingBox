#pragma warning disable 1591
using System;
using System.Threading.Tasks;

namespace EventSourcing.Internals
{
	public static partial class ReadFailureMatchExtension
	{
		public static T Match<T>(this EventSourcing.Internals.ReadFailure readFailure, Func<EventSourcing.Internals.ReadFailure.Temporary_, T> temporary, Func<EventSourcing.Internals.ReadFailure.Permanent_, T> permanent) =>
		readFailure switch
		{
			EventSourcing.Internals.ReadFailure.Temporary_ case1 => temporary(case1),
			EventSourcing.Internals.ReadFailure.Permanent_ case2 => permanent(case2),
			_ => throw new ArgumentException($"Unknown type derived from EventSourcing.Internals.ReadFailure: {readFailure.GetType().Name}")
		};
		
		public static Task<T> Match<T>(this EventSourcing.Internals.ReadFailure readFailure, Func<EventSourcing.Internals.ReadFailure.Temporary_, Task<T>> temporary, Func<EventSourcing.Internals.ReadFailure.Permanent_, Task<T>> permanent) =>
		readFailure switch
		{
			EventSourcing.Internals.ReadFailure.Temporary_ case1 => temporary(case1),
			EventSourcing.Internals.ReadFailure.Permanent_ case2 => permanent(case2),
			_ => throw new ArgumentException($"Unknown type derived from EventSourcing.Internals.ReadFailure: {readFailure.GetType().Name}")
		};
		
		public static async Task<T> Match<T>(this Task<EventSourcing.Internals.ReadFailure> readFailure, Func<EventSourcing.Internals.ReadFailure.Temporary_, T> temporary, Func<EventSourcing.Internals.ReadFailure.Permanent_, T> permanent) =>
		(await readFailure.ConfigureAwait(false)).Match(temporary, permanent);
		
		public static async Task<T> Match<T>(this Task<EventSourcing.Internals.ReadFailure> readFailure, Func<EventSourcing.Internals.ReadFailure.Temporary_, Task<T>> temporary, Func<EventSourcing.Internals.ReadFailure.Permanent_, Task<T>> permanent) =>
		await (await readFailure.ConfigureAwait(false)).Match(temporary, permanent).ConfigureAwait(false);
		
		public static void Switch(this EventSourcing.Internals.ReadFailure readFailure, Action<EventSourcing.Internals.ReadFailure.Temporary_> temporary, Action<EventSourcing.Internals.ReadFailure.Permanent_> permanent)
		{
			switch (readFailure)
			{
				case EventSourcing.Internals.ReadFailure.Temporary_ case1:
					temporary(case1);
					break;
				case EventSourcing.Internals.ReadFailure.Permanent_ case2:
					permanent(case2);
					break;
				default:
					throw new ArgumentException($"Unknown type derived from EventSourcing.Internals.ReadFailure: {readFailure.GetType().Name}");
			}
		}
		
		public static async Task Switch(this EventSourcing.Internals.ReadFailure readFailure, Func<EventSourcing.Internals.ReadFailure.Temporary_, Task> temporary, Func<EventSourcing.Internals.ReadFailure.Permanent_, Task> permanent)
		{
			switch (readFailure)
			{
				case EventSourcing.Internals.ReadFailure.Temporary_ case1:
					await temporary(case1).ConfigureAwait(false);
					break;
				case EventSourcing.Internals.ReadFailure.Permanent_ case2:
					await permanent(case2).ConfigureAwait(false);
					break;
				default:
					throw new ArgumentException($"Unknown type derived from EventSourcing.Internals.ReadFailure: {readFailure.GetType().Name}");
			}
		}
		
		public static async Task Switch(this Task<EventSourcing.Internals.ReadFailure> readFailure, Action<EventSourcing.Internals.ReadFailure.Temporary_> temporary, Action<EventSourcing.Internals.ReadFailure.Permanent_> permanent) =>
		(await readFailure.ConfigureAwait(false)).Switch(temporary, permanent);
		
		public static async Task Switch(this Task<EventSourcing.Internals.ReadFailure> readFailure, Func<EventSourcing.Internals.ReadFailure.Temporary_, Task> temporary, Func<EventSourcing.Internals.ReadFailure.Permanent_, Task> permanent) =>
		await (await readFailure.ConfigureAwait(false)).Switch(temporary, permanent).ConfigureAwait(false);
	}
}
#pragma warning restore 1591
