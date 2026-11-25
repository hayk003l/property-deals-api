namespace PropertyDeals.Domain.Exceptions.Participants;

public sealed class ParticipantAlreadyExistsException : DomainException
{
    public int UserId { get; }

    public ParticipantAlreadyExistsException(int userId)
        : base("deal.participant_already_exists", $"User {userId} is already a participant of this deal.")
    {
        UserId = userId;
    }
}
