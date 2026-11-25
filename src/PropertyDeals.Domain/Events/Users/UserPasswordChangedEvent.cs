namespace PropertyDeals.Domain.Events.Users;

public sealed record UserPasswordChangedEvent(int UserId, int ChangedBy, DateTime OccurredOnUtc) : DomainEvent(OccurredOnUtc);

