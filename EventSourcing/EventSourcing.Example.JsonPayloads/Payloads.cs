using EventSourcing.Events;

namespace EventSourcing.Example.JsonPayloads;

[SerializedEventPayload(Domain.Events.EventTypes.AccountCreated)]
public record AccountCreated(string AccountId, string Owner, decimal InitialBalance);
[SerializedEventPayload(Domain.Events.EventTypes.PaymentReceived)]
public record PaymentReceived(string AccountId, decimal Amount);
[SerializedEventPayload(Domain.Events.EventTypes.PaymentMade)]
public record PaymentMade(string AccountId, decimal Amount);

public class AccountCreatedMapper : EventPayloadMapper<AccountCreated, Domain.Events.AccountCreated>
{
	protected override Domain.Events.AccountCreated MapFromSerializablePayload(AccountCreated serialized) => new (serialized.AccountId, serialized.Owner, serialized.InitialBalance);

	protected override AccountCreated MapToSerializablePayload(Domain.Events.AccountCreated payload) => new(payload.AccountId, payload.Owner, payload.InitialBalance);
}

public class PaymentReceivedMapper : EventPayloadMapper<PaymentReceived, Domain.Events.PaymentReceived>
{
	protected override Domain.Events.PaymentReceived MapFromSerializablePayload(PaymentReceived serialized) => new (serialized.AccountId, serialized.Amount);

	protected override PaymentReceived MapToSerializablePayload(Domain.Events.PaymentReceived payload) => new(payload.AccountId, payload.Amount);
}

public class PaymentMadeMapper : EventPayloadMapper<PaymentMade, Domain.Events.PaymentMade>
{
	protected override Domain.Events.PaymentMade MapFromSerializablePayload(PaymentMade serialized) => new (serialized.AccountId, serialized.Amount);

	protected override PaymentMade MapToSerializablePayload(Domain.Events.PaymentMade payload) => new(payload.AccountId, payload.Amount);
}