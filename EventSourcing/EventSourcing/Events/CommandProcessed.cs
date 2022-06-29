using EventSourcing.Commands;
using FunicularSwitch;

namespace EventSourcing.Events;

public record CommandProcessed(CommandId CommandId, OperationResult<Unit> OperationResult, string? ResultMessage)
	: EventPayload(StreamIds.Command, EventTypes.CommandProcessed)
{
	public override string ToString() => $"{CommandId} processed with result {OperationResult}: {ResultMessage}";
}