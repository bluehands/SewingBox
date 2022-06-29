using EventSourcing.Events;
using FunicularSwitch.Generators;

namespace Example.Domain.Events;

[UnionType]
public abstract record AccountPayload(string AccountId, string EventType) : EventPayload(StreamIds.Account(AccountId), EventType);

public record AccountCreated(string AccountId, string Owner, decimal InitialBalance) : AccountPayload(AccountId, EventTypes.AccountCreated);

public record PaymentReceived(string AccountId, decimal Amount) : AccountPayload(AccountId, EventTypes.PaymentReceived);
public record PaymentMade(string AccountId, decimal Amount) : AccountPayload(AccountId, EventTypes.PaymentMade);