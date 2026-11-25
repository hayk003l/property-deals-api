using PropertyDeals.Domain.Entities;

namespace PropertyDeals.Domain.Events.Documents;

public sealed record DocumentCreatedEvent(Document Document, DateTime OccurredOnUtc) : DomainEvent(OccurredOnUtc);

