#pragma warning disable 1591
using System;
using System.Threading.Tasks;

namespace EventSourcing.Commands
{
	public static partial class FailureMatchExtension
	{
		public static T Match<T>(this EventSourcing.Commands.Failure failure, Func<EventSourcing.Commands.Failure.Cancelled_, T> cancelled, Func<EventSourcing.Commands.Failure.Conflict_, T> conflict, Func<EventSourcing.Commands.Failure.Forbidden_, T> forbidden, Func<EventSourcing.Commands.Failure.Internal_, T> @internal, Func<EventSourcing.Commands.Failure.InvalidInput_, T> invalidInput, Func<EventSourcing.Commands.Failure.Multiple_, T> multiple, Func<EventSourcing.Commands.Failure.NotFound_, T> notFound) =>
		failure switch
		{
			EventSourcing.Commands.Failure.Cancelled_ case1 => cancelled(case1),
			EventSourcing.Commands.Failure.Conflict_ case2 => conflict(case2),
			EventSourcing.Commands.Failure.Forbidden_ case3 => forbidden(case3),
			EventSourcing.Commands.Failure.Internal_ case4 => @internal(case4),
			EventSourcing.Commands.Failure.InvalidInput_ case5 => invalidInput(case5),
			EventSourcing.Commands.Failure.Multiple_ case6 => multiple(case6),
			EventSourcing.Commands.Failure.NotFound_ case7 => notFound(case7),
			_ => throw new ArgumentException($"Unknown type derived from EventSourcing.Commands.Failure: {failure.GetType().Name}")
		};
		
		public static Task<T> Match<T>(this EventSourcing.Commands.Failure failure, Func<EventSourcing.Commands.Failure.Cancelled_, Task<T>> cancelled, Func<EventSourcing.Commands.Failure.Conflict_, Task<T>> conflict, Func<EventSourcing.Commands.Failure.Forbidden_, Task<T>> forbidden, Func<EventSourcing.Commands.Failure.Internal_, Task<T>> @internal, Func<EventSourcing.Commands.Failure.InvalidInput_, Task<T>> invalidInput, Func<EventSourcing.Commands.Failure.Multiple_, Task<T>> multiple, Func<EventSourcing.Commands.Failure.NotFound_, Task<T>> notFound) =>
		failure switch
		{
			EventSourcing.Commands.Failure.Cancelled_ case1 => cancelled(case1),
			EventSourcing.Commands.Failure.Conflict_ case2 => conflict(case2),
			EventSourcing.Commands.Failure.Forbidden_ case3 => forbidden(case3),
			EventSourcing.Commands.Failure.Internal_ case4 => @internal(case4),
			EventSourcing.Commands.Failure.InvalidInput_ case5 => invalidInput(case5),
			EventSourcing.Commands.Failure.Multiple_ case6 => multiple(case6),
			EventSourcing.Commands.Failure.NotFound_ case7 => notFound(case7),
			_ => throw new ArgumentException($"Unknown type derived from EventSourcing.Commands.Failure: {failure.GetType().Name}")
		};
		
		public static async Task<T> Match<T>(this Task<EventSourcing.Commands.Failure> failure, Func<EventSourcing.Commands.Failure.Cancelled_, T> cancelled, Func<EventSourcing.Commands.Failure.Conflict_, T> conflict, Func<EventSourcing.Commands.Failure.Forbidden_, T> forbidden, Func<EventSourcing.Commands.Failure.Internal_, T> @internal, Func<EventSourcing.Commands.Failure.InvalidInput_, T> invalidInput, Func<EventSourcing.Commands.Failure.Multiple_, T> multiple, Func<EventSourcing.Commands.Failure.NotFound_, T> notFound) =>
		(await failure.ConfigureAwait(false)).Match(cancelled, conflict, forbidden, @internal, invalidInput, multiple, notFound);
		
		public static async Task<T> Match<T>(this Task<EventSourcing.Commands.Failure> failure, Func<EventSourcing.Commands.Failure.Cancelled_, Task<T>> cancelled, Func<EventSourcing.Commands.Failure.Conflict_, Task<T>> conflict, Func<EventSourcing.Commands.Failure.Forbidden_, Task<T>> forbidden, Func<EventSourcing.Commands.Failure.Internal_, Task<T>> @internal, Func<EventSourcing.Commands.Failure.InvalidInput_, Task<T>> invalidInput, Func<EventSourcing.Commands.Failure.Multiple_, Task<T>> multiple, Func<EventSourcing.Commands.Failure.NotFound_, Task<T>> notFound) =>
		await (await failure.ConfigureAwait(false)).Match(cancelled, conflict, forbidden, @internal, invalidInput, multiple, notFound).ConfigureAwait(false);
		
		public static void Switch(this EventSourcing.Commands.Failure failure, Action<EventSourcing.Commands.Failure.Cancelled_> cancelled, Action<EventSourcing.Commands.Failure.Conflict_> conflict, Action<EventSourcing.Commands.Failure.Forbidden_> forbidden, Action<EventSourcing.Commands.Failure.Internal_> @internal, Action<EventSourcing.Commands.Failure.InvalidInput_> invalidInput, Action<EventSourcing.Commands.Failure.Multiple_> multiple, Action<EventSourcing.Commands.Failure.NotFound_> notFound)
		{
			switch (failure)
			{
				case EventSourcing.Commands.Failure.Cancelled_ case1:
					cancelled(case1);
					break;
				case EventSourcing.Commands.Failure.Conflict_ case2:
					conflict(case2);
					break;
				case EventSourcing.Commands.Failure.Forbidden_ case3:
					forbidden(case3);
					break;
				case EventSourcing.Commands.Failure.Internal_ case4:
					@internal(case4);
					break;
				case EventSourcing.Commands.Failure.InvalidInput_ case5:
					invalidInput(case5);
					break;
				case EventSourcing.Commands.Failure.Multiple_ case6:
					multiple(case6);
					break;
				case EventSourcing.Commands.Failure.NotFound_ case7:
					notFound(case7);
					break;
				default:
					throw new ArgumentException($"Unknown type derived from EventSourcing.Commands.Failure: {failure.GetType().Name}");
			}
		}
		
		public static async Task Switch(this EventSourcing.Commands.Failure failure, Func<EventSourcing.Commands.Failure.Cancelled_, Task> cancelled, Func<EventSourcing.Commands.Failure.Conflict_, Task> conflict, Func<EventSourcing.Commands.Failure.Forbidden_, Task> forbidden, Func<EventSourcing.Commands.Failure.Internal_, Task> @internal, Func<EventSourcing.Commands.Failure.InvalidInput_, Task> invalidInput, Func<EventSourcing.Commands.Failure.Multiple_, Task> multiple, Func<EventSourcing.Commands.Failure.NotFound_, Task> notFound)
		{
			switch (failure)
			{
				case EventSourcing.Commands.Failure.Cancelled_ case1:
					await cancelled(case1).ConfigureAwait(false);
					break;
				case EventSourcing.Commands.Failure.Conflict_ case2:
					await conflict(case2).ConfigureAwait(false);
					break;
				case EventSourcing.Commands.Failure.Forbidden_ case3:
					await forbidden(case3).ConfigureAwait(false);
					break;
				case EventSourcing.Commands.Failure.Internal_ case4:
					await @internal(case4).ConfigureAwait(false);
					break;
				case EventSourcing.Commands.Failure.InvalidInput_ case5:
					await invalidInput(case5).ConfigureAwait(false);
					break;
				case EventSourcing.Commands.Failure.Multiple_ case6:
					await multiple(case6).ConfigureAwait(false);
					break;
				case EventSourcing.Commands.Failure.NotFound_ case7:
					await notFound(case7).ConfigureAwait(false);
					break;
				default:
					throw new ArgumentException($"Unknown type derived from EventSourcing.Commands.Failure: {failure.GetType().Name}");
			}
		}
		
		public static async Task Switch(this Task<EventSourcing.Commands.Failure> failure, Action<EventSourcing.Commands.Failure.Cancelled_> cancelled, Action<EventSourcing.Commands.Failure.Conflict_> conflict, Action<EventSourcing.Commands.Failure.Forbidden_> forbidden, Action<EventSourcing.Commands.Failure.Internal_> @internal, Action<EventSourcing.Commands.Failure.InvalidInput_> invalidInput, Action<EventSourcing.Commands.Failure.Multiple_> multiple, Action<EventSourcing.Commands.Failure.NotFound_> notFound) =>
		(await failure.ConfigureAwait(false)).Switch(cancelled, conflict, forbidden, @internal, invalidInput, multiple, notFound);
		
		public static async Task Switch(this Task<EventSourcing.Commands.Failure> failure, Func<EventSourcing.Commands.Failure.Cancelled_, Task> cancelled, Func<EventSourcing.Commands.Failure.Conflict_, Task> conflict, Func<EventSourcing.Commands.Failure.Forbidden_, Task> forbidden, Func<EventSourcing.Commands.Failure.Internal_, Task> @internal, Func<EventSourcing.Commands.Failure.InvalidInput_, Task> invalidInput, Func<EventSourcing.Commands.Failure.Multiple_, Task> multiple, Func<EventSourcing.Commands.Failure.NotFound_, Task> notFound) =>
		await (await failure.ConfigureAwait(false)).Switch(cancelled, conflict, forbidden, @internal, invalidInput, multiple, notFound).ConfigureAwait(false);
	}
}
#pragma warning restore 1591
