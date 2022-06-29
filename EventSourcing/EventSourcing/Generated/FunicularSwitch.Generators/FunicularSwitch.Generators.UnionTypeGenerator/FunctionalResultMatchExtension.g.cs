using System;
using System.Threading.Tasks;

namespace EventSourcing.Commands
{
	public static partial class MatchExtension
	{
		public static T Match<T>(this EventSourcing.Commands.FunctionalResult functionalResult, Func<EventSourcing.Commands.FunctionalResult.Failed_, T> failed, Func<EventSourcing.Commands.FunctionalResult.Ok_, T> ok) =>
		functionalResult switch
		{
			EventSourcing.Commands.FunctionalResult.Failed_ case1 => failed(case1),
			EventSourcing.Commands.FunctionalResult.Ok_ case2 => ok(case2),
			_ => throw new ArgumentException($"Unknown type derived from EventSourcing.Commands.FunctionalResult: {functionalResult.GetType().Name}")
		};
		
		public static Task<T> Match<T>(this EventSourcing.Commands.FunctionalResult functionalResult, Func<EventSourcing.Commands.FunctionalResult.Failed_, Task<T>> failed, Func<EventSourcing.Commands.FunctionalResult.Ok_, Task<T>> ok) =>
		functionalResult switch
		{
			EventSourcing.Commands.FunctionalResult.Failed_ case1 => failed(case1),
			EventSourcing.Commands.FunctionalResult.Ok_ case2 => ok(case2),
			_ => throw new ArgumentException($"Unknown type derived from EventSourcing.Commands.FunctionalResult: {functionalResult.GetType().Name}")
		};
		
		public static async Task<T> Match<T>(this Task<EventSourcing.Commands.FunctionalResult> functionalResult, Func<EventSourcing.Commands.FunctionalResult.Failed_, T> failed, Func<EventSourcing.Commands.FunctionalResult.Ok_, T> ok) =>
		(await functionalResult.ConfigureAwait(false)).Match(failed, ok);
		
		public static async Task<T> Match<T>(this Task<EventSourcing.Commands.FunctionalResult> functionalResult, Func<EventSourcing.Commands.FunctionalResult.Failed_, Task<T>> failed, Func<EventSourcing.Commands.FunctionalResult.Ok_, Task<T>> ok) =>
		await (await functionalResult.ConfigureAwait(false)).Match(failed, ok).ConfigureAwait(false);
		
		public static void Switch(this EventSourcing.Commands.FunctionalResult functionalResult, Action<EventSourcing.Commands.FunctionalResult.Failed_> failed, Action<EventSourcing.Commands.FunctionalResult.Ok_> ok)
		{
			switch (functionalResult)
			{
				case EventSourcing.Commands.FunctionalResult.Failed_ case1:
					failed(case1);
					break;
				case EventSourcing.Commands.FunctionalResult.Ok_ case2:
					ok(case2);
					break;
				default:
					throw new ArgumentException($"Unknown type derived from EventSourcing.Commands.FunctionalResult: {functionalResult.GetType().Name}");
			}
		}
		
		public static async Task Switch(this EventSourcing.Commands.FunctionalResult functionalResult, Func<EventSourcing.Commands.FunctionalResult.Failed_, Task> failed, Func<EventSourcing.Commands.FunctionalResult.Ok_, Task> ok)
		{
			switch (functionalResult)
			{
				case EventSourcing.Commands.FunctionalResult.Failed_ case1:
					await failed(case1).ConfigureAwait(false);
					break;
				case EventSourcing.Commands.FunctionalResult.Ok_ case2:
					await ok(case2).ConfigureAwait(false);
					break;
				default:
					throw new ArgumentException($"Unknown type derived from EventSourcing.Commands.FunctionalResult: {functionalResult.GetType().Name}");
			}
		}
		
		public static async Task Switch(this Task<EventSourcing.Commands.FunctionalResult> functionalResult, Action<EventSourcing.Commands.FunctionalResult.Failed_> failed, Action<EventSourcing.Commands.FunctionalResult.Ok_> ok) =>
		(await functionalResult.ConfigureAwait(false)).Switch(failed, ok);
		
		public static async Task Switch(this Task<EventSourcing.Commands.FunctionalResult> functionalResult, Func<EventSourcing.Commands.FunctionalResult.Failed_, Task> failed, Func<EventSourcing.Commands.FunctionalResult.Ok_, Task> ok) =>
		await (await functionalResult.ConfigureAwait(false)).Switch(failed, ok).ConfigureAwait(false);
	}
}
