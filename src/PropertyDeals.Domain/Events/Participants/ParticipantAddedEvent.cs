using PropertyDeals.Domain.Enums;

namespace PropertyDeals.Domain.Events.Participants;

public sealed record ParticipantAddedEvent(
    int DealId,
    int UserId,
    ParticipantRoleEnum Role,
    int AddedBy,
    DateTime OccurredOnUtc) : DomainEvent(OccurredOnUtc);
