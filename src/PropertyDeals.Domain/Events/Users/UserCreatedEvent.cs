using PropertyDeals.Domain.Entities;

namespace PropertyDeals.Domain.Events.Users;

public sealed record UserCreatedEvent(User User, DateTime OccurredOnUtc) : DomainEvent(OccurredOnUtc);
