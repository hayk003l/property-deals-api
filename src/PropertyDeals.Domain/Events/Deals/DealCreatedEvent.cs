using PropertyDeals.Domain.Entities;

namespace PropertyDeals.Domain.Events.Deals;

public sealed record DealCreatedEvent(Deal Deal, DateTime OccurredOnUtc) : DomainEvent(OccurredOnUtc);
