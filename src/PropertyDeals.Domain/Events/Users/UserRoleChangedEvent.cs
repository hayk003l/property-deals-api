using PropertyDeals.Domain.Enums;

namespace PropertyDeals.Domain.Events.Users;

public sealed record UserRoleChangedEvent(int UserId, RolesEnum OldRole, RolesEnum NewRole, int ChangedBy, DateTime OccurredOnUtc) : DomainEvent(OccurredOnUtc);

