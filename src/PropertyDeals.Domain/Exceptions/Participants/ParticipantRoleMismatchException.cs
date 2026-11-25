using PropertyDeals.Domain.Enums;

namespace PropertyDeals.Domain.Exceptions.Participants;

/// <summary>The user's account role does not allow the requested deal role (e.g. a Client as ListingAgent).</summary>
public sealed class ParticipantRoleMismatchException : DomainException
{
    public int UserId { get; }
    public RolesEnum UserRole { get; }
    public ParticipantRoleEnum ParticipantRole { get; }

    public ParticipantRoleMismatchException(int userId, RolesEnum userRole, ParticipantRoleEnum participantRole)
        : base("deal.participant_role_mismatch",
               $"User {userId} with role {userRole} cannot be a {participantRole} on a deal.")
    {
        UserId = userId;
        UserRole = userRole;
        ParticipantRole = participantRole;
    }
}
