using PropertyDeals.Domain.Enums;

namespace PropertyDeals.Domain.Events.Deals;

public sealed record DealStatusChangedEvent(
    int DealId,
    DealStatusEnum OldStatus,
    DealStatusEnum NewStatus,
    int ChangedBy,
    DateTime OccurredOnUtc) : DomainEvent(OccurredOnUtc);
