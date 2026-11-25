using PropertyDeals.Domain.Entities;

namespace PropertyDeals.Domain.Events.Properties;

public sealed record PropertyCreatedEvent(Property Property, DateTime OccurredOnUtc) : DomainEvent(OccurredOnUtc);

