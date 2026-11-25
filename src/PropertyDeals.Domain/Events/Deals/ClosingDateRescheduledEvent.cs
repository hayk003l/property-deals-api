namespace PropertyDeals.Domain.Events.Deals;

public sealed record ClosingDateRescheduledEvent(
    int DealId,
    DateOnly OldClosingDate,
    DateOnly NewClosingDate,
    int ChangedBy,
    DateTime OccurredOnUtc) : DomainEvent(OccurredOnUtc);
