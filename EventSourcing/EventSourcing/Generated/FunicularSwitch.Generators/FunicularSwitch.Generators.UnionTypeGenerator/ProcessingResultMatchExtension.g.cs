using System;
using System.Threading.Tasks;

namespace EventSourcing.Commands
{
	public static partial class MatchExtension
	{
		public static T Match<T>(this EventSourcing.Commands.ProcessingResult processingResult, Func<EventSourcing.Commands.ProcessingResult.Cancelled_, T> cancelled, Func<EventSourcing.Commands.ProcessingResult.Faulted_, T> faulted, Func<EventSourcing.Commands.ProcessingResult.Processed_, T> processed, Func<EventSourcing.Commands.ProcessingResult.Unhandled_, T> unhandled) =>
		processingResult switch
		{
			EventSourcing.Commands.ProcessingResult.Cancelled_ case1 => cancelled(case1),
			EventSourcing.Commands.ProcessingResult.Faulted_ case2 => faulted(case2),
			EventSourcing.Commands.ProcessingResult.Processed_ case3 => processed(case3),
			EventSourcing.Commands.ProcessingResult.Unhandled_ case4 => unhandled(case4),
			_ => throw new ArgumentException($"Unknown type derived from EventSourcing.Commands.ProcessingResult: {processingResult.GetType().Name}")
		};
		
		public static Task<T> Match<T>(this EventSourcing.Commands.ProcessingResult processingResult, Func<EventSourcing.Commands.ProcessingResult.Cancelled_, Task<T>> cancelled, Func<EventSourcing.Commands.ProcessingResult.Faulted_, Task<T>> faulted, Func<EventSourcing.Commands.ProcessingResult.Processed_, Task<T>> processed, Func<EventSourcing.Commands.ProcessingResult.Unhandled_, Task<T>> unhandled) =>
		processingResult switch
		{
			EventSourcing.Commands.ProcessingResult.Cancelled_ case1 => cancelled(case1),
			EventSourcing.Commands.ProcessingResult.Faulted_ case2 => faulted(case2),
			EventSourcing.Commands.ProcessingResult.Processed_ case3 => processed(case3),
			EventSourcing.Commands.ProcessingResult.Unhandled_ case4 => unhandled(case4),
			_ => throw new ArgumentException($"Unknown type derived from EventSourcing.Commands.ProcessingResult: {processingResult.GetType().Name}")
		};
		
		public static async Task<T> Match<T>(this Task<EventSourcing.Commands.ProcessingResult> processingResult, Func<EventSourcing.Commands.ProcessingResult.Cancelled_, T> cancelled, Func<EventSourcing.Commands.ProcessingResult.Faulted_, T> faulted, Func<EventSourcing.Commands.ProcessingResult.Processed_, T> processed, Func<EventSourcing.Commands.ProcessingResult.Unhandled_, T> unhandled) =>
		(await processingResult.ConfigureAwait(false)).Match(cancelled, faulted, processed, unhandled);
		
		public static async Task<T> Match<T>(this Task<EventSourcing.Commands.ProcessingResult> processingResult, Func<EventSourcing.Commands.ProcessingResult.Cancelled_, Task<T>> cancelled, Func<EventSourcing.Commands.ProcessingResult.Faulted_, Task<T>> faulted, Func<EventSourcing.Commands.ProcessingResult.Processed_, Task<T>> processed, Func<EventSourcing.Commands.ProcessingResult.Unhandled_, Task<T>> unhandled) =>
		await (await processingResult.ConfigureAwait(false)).Match(cancelled, faulted, processed, unhandled).ConfigureAwait(false);
		
		public static void Switch(this EventSourcing.Commands.ProcessingResult processingResult, Action<EventSourcing.Commands.ProcessingResult.Cancelled_> cancelled, Action<EventSourcing.Commands.ProcessingResult.Faulted_> faulted, Action<EventSourcing.Commands.ProcessingResult.Processed_> processed, Action<EventSourcing.Commands.ProcessingResult.Unhandled_> unhandled)
		{
			switch (processingResult)
			{
				case EventSourcing.Commands.ProcessingResult.Cancelled_ case1:
					cancelled(case1);
					break;
				case EventSourcing.Commands.ProcessingResult.Faulted_ case2:
					faulted(case2);
					break;
				case EventSourcing.Commands.ProcessingResult.Processed_ case3:
					processed(case3);
					break;
				case EventSourcing.Commands.ProcessingResult.Unhandled_ case4:
					unhandled(case4);
					break;
				default:
					throw new ArgumentException($"Unknown type derived from EventSourcing.Commands.ProcessingResult: {processingResult.GetType().Name}");
			}
		}
		
		public static async Task Switch(this EventSourcing.Commands.ProcessingResult processingResult, Func<EventSourcing.Commands.ProcessingResult.Cancelled_, Task> cancelled, Func<EventSourcing.Commands.ProcessingResult.Faulted_, Task> faulted, Func<EventSourcing.Commands.ProcessingResult.Processed_, Task> processed, Func<EventSourcing.Commands.ProcessingResult.Unhandled_, Task> unhandled)
		{
			switch (processingResult)
			{
				case EventSourcing.Commands.ProcessingResult.Cancelled_ case1:
					await cancelled(case1).ConfigureAwait(false);
					break;
				case EventSourcing.Commands.ProcessingResult.Faulted_ case2:
					await faulted(case2).ConfigureAwait(false);
					break;
				case EventSourcing.Commands.ProcessingResult.Processed_ case3:
					await processed(case3).ConfigureAwait(false);
					break;
				case EventSourcing.Commands.ProcessingResult.Unhandled_ case4:
					await unhandled(case4).ConfigureAwait(false);
					break;
				default:
					throw new ArgumentException($"Unknown type derived from EventSourcing.Commands.ProcessingResult: {processingResult.GetType().Name}");
			}
		}
		
		public static async Task Switch(this Task<EventSourcing.Commands.ProcessingResult> processingResult, Action<EventSourcing.Commands.ProcessingResult.Cancelled_> cancelled, Action<EventSourcing.Commands.ProcessingResult.Faulted_> faulted, Action<EventSourcing.Commands.ProcessingResult.Processed_> processed, Action<EventSourcing.Commands.ProcessingResult.Unhandled_> unhandled) =>
		(await processingResult.ConfigureAwait(false)).Switch(cancelled, faulted, processed, unhandled);
		
		public static async Task Switch(this Task<EventSourcing.Commands.ProcessingResult> processingResult, Func<EventSourcing.Commands.ProcessingResult.Cancelled_, Task> cancelled, Func<EventSourcing.Commands.ProcessingResult.Faulted_, Task> faulted, Func<EventSourcing.Commands.ProcessingResult.Processed_, Task> processed, Func<EventSourcing.Commands.ProcessingResult.Unhandled_, Task> unhandled) =>
		await (await processingResult.ConfigureAwait(false)).Switch(cancelled, faulted, processed, unhandled).ConfigureAwait(false);
	}
}
