namespace PropertyDeals.Domain.Events.Documents;

public sealed record DocumentUploadedEvent(int DocumentId, int DealId, int UploadedBy, DateTime OccurredOnUtc) : DomainEvent(OccurredOnUtc);
