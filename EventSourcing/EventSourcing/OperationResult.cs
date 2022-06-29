using EventSourcing.Commands;
using FunicularSwitch.Generators;

namespace EventSourcing;

[ResultType(errorType: typeof(Failure))]
public abstract partial class OperationResult
{
	public static OperationResult<T> InvalidInput<T>(string message) => Error<T>(Failure.InvalidInput(message));
	public static OperationResult<T> Forbidden<T>(string message) => Error<T>(Failure.Forbidden(message));
	public static OperationResult<T> Conflict<T>(string message) => Error<T>(Failure.Conflict(message));
	public static OperationResult<T> NotFound<T>(string message) => Error<T>(Failure.NotFound(message));
	public static OperationResult<T> InternalError<T>(string message) => Error<T>(Failure.Internal(message));
	public static OperationResult<T> Cancelled<T>(string? message = null) => Error<T>(Failure.Cancelled(message));
}