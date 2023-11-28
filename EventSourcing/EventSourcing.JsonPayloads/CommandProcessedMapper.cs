using System;
using System.Collections.Immutable;
using System.Linq;
using EventSourcing.Commands;
using EventSourcing.Events;
using FunicularSwitch;

namespace EventSourcing.JsonPayloads;

public class CommandProcessedMapper : EventPayloadMapper<CommandProcessed, Events.CommandProcessed>
{
	protected override Events.CommandProcessed MapFromSerializablePayload(CommandProcessed serialized) =>
		new(CommandId: new(Id: serialized.CommandId),
			OperationResult: serialized.OperationResult.UnionCase == OperationResultUnionCases.Ok
				? OperationResult.Ok(value: No.Thing)
				: OperationResult.Error<Unit>(details: Map(failure: serialized.OperationResult.Failure!)),
			ResultMessage: serialized.ResultMessage);

	static Commands.Failure Map(Failure failure)
	{
		var (unionCases, message, childFailures) = failure;
		return unionCases switch
		{
			Failure.UnionCases.Cancelled => Commands.Failure.Cancelled(message: message),
			Failure.UnionCases.Conflict => Commands.Failure.Conflict(message: message!),
			Failure.UnionCases.Forbidden => Commands.Failure.Forbidden(message: message!),
			Failure.UnionCases.Internal => Commands.Failure.Internal(message: message!),
			Failure.UnionCases.InvalidInput => Commands.Failure.InvalidInput(message: message!),
			Failure.UnionCases.NotFound => Commands.Failure.NotFound(message: message!),
			Failure.UnionCases.Multiple => Commands.Failure.Multiple(failures: childFailures!.Select(Map).ToImmutableArray()),
			_ => throw new ArgumentOutOfRangeException()
		};
	}

	static Failure Map(Commands.Failure failure) =>
		failure.Match(
			cancelled: _ => new(UnionCase: Failure.UnionCases.Cancelled, Message: failure.Message),
			conflict: _ => new(UnionCase: Failure.UnionCases.Conflict, Message: failure.Message),
			forbidden: _ => new(UnionCase: Failure.UnionCases.Forbidden, Message: failure.Message),
			@internal: _ => new(UnionCase: Failure.UnionCases.Internal, Message: failure.Message),
			invalidInput: _ => new(UnionCase: Failure.UnionCases.InvalidInput, Message: failure.Message),
			notFound: _ => new (UnionCase: Failure.UnionCases.NotFound, Message: failure.Message),
			multiple: m => new Failure(UnionCase: Failure.UnionCases.Multiple, Message: null, m.Failures.Select(Map).ToImmutableArray())
		);

	protected override CommandProcessed MapToSerializablePayload(Events.CommandProcessed payload) => new(
		CommandId: payload.CommandId.Id,
		OperationResult: Map(source: payload.OperationResult),
		ResultMessage: payload.ResultMessage
	);

	static OperationResult<bool> Map(EventSourcing.OperationResult<Unit> source)
	{
		var (unionCase, failure) = source.Match(
			ok: _ => (OperationResultUnionCases.Ok, (Failure?)null),
			error: error => (OperationResultUnionCases.Error, Map(failure: error))
		);
		return new(UnionCase: unionCase, Failure: failure, Value: false);
	}
}