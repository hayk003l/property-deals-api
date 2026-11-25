using PropertyDeals.Domain.Enums;

namespace PropertyDeals.Domain.Events.Participants;

public sealed record ParticipantRemovedEvent(
    int DealId,
    int UserId,
    ParticipantRoleEnum Role,
    int RemovedBy,
    DateTime OccurredOnUtc) : DomainEvent(OccurredOnUtc);
