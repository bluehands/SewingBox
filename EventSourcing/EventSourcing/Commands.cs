using System.Collections.Immutable;

namespace EventSourcing;

public delegate CommandProcessor? GetCommandProcessor(Type commandType);

public abstract record Command
{
	public CommandId Id { get; } = CommandId.NewCommandId();

	public override string ToString() => $"{GetType().Name} ({Id.Id})";
}

public record CommandId(Guid Id)
{
	public static CommandId NewCommandId() => new(Guid.NewGuid());
	public override string ToString() => Id.ToString("N");
}

public abstract class CommandProcessor
{
	public static async Task<ProcessingResult> Process(Command command, GetCommandProcessor getCommandProcessor)
	{
		try
		{
			var commandProcessor = getCommandProcessor(command.GetType());
			var processingResult = commandProcessor != null ?
				await commandProcessor.Process(command).ConfigureAwait(false) :
				ProcessingResult.Unhandled(command.Id, $"No command processor registered for command {command.GetType().Name}");
			return processingResult;
		}
		catch (OperationCanceledException)
		{
			return ProcessingResult.Cancelled(command.Id, "Command was cancelled");
		}
		catch (Exception e)
		{
			return ProcessingResult.Faulted(e, command.Id);
		}
	}

	protected abstract Task<ProcessingResult.Processed_> Process(Command command);
}

public abstract class CommandProcessor<T> : CommandProcessor where T : Command
{
	protected override async Task<ProcessingResult.Processed_> Process(Command command) => await InternalProcess((T)command).ConfigureAwait(false);

	public abstract Task<ProcessingResult.Processed_> InternalProcess(T command);
}

public abstract class SynchronousCommandProcessor<T> : CommandProcessor<T> where T : Command
{
	public override Task<ProcessingResult.Processed_> InternalProcess(T command) =>
		Task.FromResult(InternalProcessSync(command));

	public abstract ProcessingResult.Processed_ InternalProcessSync(T command);
}

[FunicularSwitch.Generators.UnionType]
public abstract class ProcessingResult
{
	public static ProcessingResult Processed(EventPayload resultEvent, CommandId commandId, FunctionalResult functionalResult) => new Processed_(resultEvent, commandId, functionalResult);
	public static ProcessingResult Unhandled(CommandId commandId, string message = "No command processor registered") => new Unhandled_(commandId, message);
	public static ProcessingResult Faulted(Exception exception, CommandId commandId, string? resultMessage = null) => new Faulted_(exception, commandId, resultMessage);
	public static ProcessingResult Cancelled(CommandId commandId, string message = "Operation cancelled") => new Cancelled_(commandId, message);

	public CommandId CommandId { get; }

	public UnionCases UnionCase { get; }

	public string? ResultMessage { get; }

	ProcessingResult(UnionCases unionCase, CommandId commandId, string? resultMessage)
	{
		CommandId = commandId;
		UnionCase = unionCase;
		ResultMessage = resultMessage;
	}

	public enum UnionCases
	{
		Processed,
		Unhandled,
		Faulted,
		Cancelled
	}

	public sealed class Processed_ : ProcessingResult
	{
		public IReadOnlyCollection<EventPayload> ResultEvents { get; }

		public FunctionalResult FunctionalResult { get; }

		public Processed_(EventPayload resultEvent, CommandId commandId, FunctionalResult functionalResult) : this(new[] { resultEvent }, commandId, functionalResult)
		{
		}

		public Processed_(IEnumerable<EventPayload> resultEvents, CommandId commandId, FunctionalResult functionalResult) : base(UnionCases.Processed, commandId, functionalResult.ResultMessage)
		{
			FunctionalResult = functionalResult;
			ResultEvents = resultEvents.ToImmutableList();
		}

		public override string ToString() => $"{nameof(FunctionalResult)}: {FunctionalResult}, Result event count: {ResultEvents.Count}";
	}

	public sealed class Faulted_ : ProcessingResult
	{
		public Exception Exception { get; }

		public Faulted_(Exception exception, CommandId commandId, string? resultMessage = null) : base(UnionCases.Faulted, commandId, resultMessage ?? exception.ToString()) => Exception = exception;

		public override string ToString() => $"Faulted: {Exception}";
	}

	public sealed class Unhandled_ : ProcessingResult
	{
		public Unhandled_(CommandId commandId, string message = "No command processor registered") : base(UnionCases.Unhandled, commandId, message)
		{
		}

		public override string ToString() => nameof(Unhandled_);
	}

	public sealed class Cancelled_ : ProcessingResult
	{
		public Cancelled_(CommandId commandId, string message = "Operation cancelled") : base(UnionCases.Cancelled, commandId, message)
		{
		}

		public override string ToString() => nameof(Cancelled_);
	}
}

[FunicularSwitch.Generators.UnionType]
public abstract class FunctionalResult
{
	public static FunctionalResult Ok(string resultMessage) => new Ok_(resultMessage);
	public static FunctionalResult Failed(Failure failure) => new Failed_(failure);

	public string ResultMessage { get; }

	public class Ok_ : FunctionalResult
	{
		public Ok_(string resultMessage) : base(UnionCases.Ok, resultMessage)
		{
		}
	}

	public class Failed_ : FunctionalResult
	{
		public Failure Failure { get; }

		public Failed_(Failure failure) : base(UnionCases.Failed, failure.Message) => Failure = failure;
	}

	internal enum UnionCases
	{
		Ok,
		Failed
	}

	internal UnionCases UnionCase { get; }
	FunctionalResult(UnionCases unionCase, string resultMessage)
	{
		UnionCase = unionCase;
		ResultMessage = resultMessage;
	}

	public override string ToString() => Enum.GetName(typeof(UnionCases), UnionCase) ?? UnionCase.ToString();
	bool Equals(FunctionalResult other) => UnionCase == other.UnionCase;

	public override bool Equals(object? obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != GetType()) return false;
		return Equals((FunctionalResult)obj);
	}

	public override int GetHashCode() => (int)UnionCase;
}

[FunicularSwitch.Generators.UnionType]
public abstract class Failure
{
	public static Failure Forbidden(string message) => new Forbidden_(message);
	public static Failure NotFound(string message) => new NotFound_(message);
	public static Failure Conflict(string message) => new Conflict_(message);
	public static Failure Internal(string message) => new Internal_(message);
	public static Failure InvalidInput(string message) => new InvalidInput_(message);
	public static Failure Cancelled(string? message = null) => new Cancelled_(message);

	public string Message { get; }

	public class Forbidden_ : Failure
	{
		public Forbidden_(string message) : base(UnionCases.Forbidden, message)
		{
		}
	}

	public class NotFound_ : Failure
	{
		public NotFound_(string message) : base(UnionCases.NotFound, message)
		{
		}
	}

	public class Conflict_ : Failure
	{
		public Conflict_(string message) : base(UnionCases.Conflict, message)
		{
		}
	}

	public class Internal_ : Failure
	{
		public Internal_(string message) : base(UnionCases.Internal, message)
		{
		}
	}

	public class InvalidInput_ : Failure
	{
		public InvalidInput_(string message) : base(UnionCases.InvalidInput, message)
		{
		}
	}

	public class Cancelled_ : Failure
	{
		public Cancelled_(string? message = null) : base(UnionCases.Cancelled, message ?? "Operation cancelled")
		{
		}
	}

	internal enum UnionCases
	{
		Forbidden,
		NotFound,
		Conflict,
		Internal,
		InvalidInput,
		Cancelled
	}

	internal UnionCases UnionCase { get; }
	Failure(UnionCases unionCase, string message)
	{
		UnionCase = unionCase;
		Message = message;
	}

	public override string ToString() => Enum.GetName(typeof(UnionCases), UnionCase) ?? UnionCase.ToString();
	bool Equals(Failure other) => UnionCase == other.UnionCase;

	public override bool Equals(object? obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != GetType()) return false;
		return Equals((Failure)obj);
	}

	public override int GetHashCode() => (int)UnionCase;
}