namespace PropertyDeals.Domain.Exceptions.Participants;

public sealed class ParticipantNotFoundException : DomainException
{
    public int UserId { get; }

    public ParticipantNotFoundException(int userId)
        : base("deal.participant_not_found", $"User {userId} is not a participant of this deal.")
    {
        UserId = userId;
    }
}
