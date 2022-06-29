using System;
using System.Collections.Generic;
using EventSourcing.Events;

namespace EventSourcing.JsonPayloads;

[SerializedEventPayload(EventTypes.CommandProcessed)]
public record CommandProcessed(Guid CommandId, OperationResult<bool> OperationResult, string? ResultMessage);

public record OperationResult<T>(OperationResultUnionCases UnionCase, Failure? Failure, T Value);

public enum OperationResultUnionCases
{
	Ok,
	Error
}

public record Failure(Failure.UnionCases UnionCase, string? Message, IReadOnlyCollection<Failure>? ChildFailures = null)
{
	public enum UnionCases
	{
		Forbidden,
		NotFound,
		Conflict,
		Internal,
		InvalidInput,
		Cancelled,
		Multiple
	}
}