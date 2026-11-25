namespace PropertyDeals.Domain.Events.Documents;

public sealed record DocumentDeletedEvent(int DocumentId, int DealId, int DeletedBy, DateTime OccurredOnUtc) : DomainEvent(OccurredOnUtc);

