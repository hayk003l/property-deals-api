using PropertyDeals.Domain.ValueObjects;

namespace PropertyDeals.Domain.Events.Properties;

public sealed record PropertyPriceChangedEvent(int PropertyId, Money OldPrice, Money NewPrice, int ChangedBy, DateTime OccurredOnUtc) : DomainEvent(OccurredOnUtc);
