using PropertyDeals.Domain.Entities.Base;
using PropertyDeals.Domain.Enums;
using PropertyDeals.Domain.Exceptions;
using PropertyDeals.Domain.Exceptions.Participants;

namespace PropertyDeals.Domain.Entities;

public class Participant : Entity
{
    private static readonly Dictionary<ParticipantRoleEnum, RolesEnum> RequiredUserRole = new()
    {
        { ParticipantRoleEnum.ListingAgent, RolesEnum.Agent },
        { ParticipantRoleEnum.BuyerAgent, RolesEnum.Agent },
        { ParticipantRoleEnum.Buyer, RolesEnum.Client },
        { ParticipantRoleEnum.Seller, RolesEnum.Client }
    };

    public int DealId { get; private set; }
    public Deal Deal { get; private set; } = null!;
    public int UserId { get; private set; }
    public ParticipantRoleEnum Role { get; private set; }

    private Participant() { }

    internal static Participant Create(Deal deal, int userId, RolesEnum userRole, ParticipantRoleEnum role)
    {
        if (deal is null)
        {
            throw new ArgumentNullException(nameof(deal));
        }

        if (userId <= 0)
        {
            throw new DomainValidationException("User id must be positive.");
        }

        if (!Enum.IsDefined(userRole))
        {
            throw new DomainValidationException($"'{userRole}' is not a valid user role.");
        }

        if (!Enum.IsDefined(role))
        {
            throw new DomainValidationException($"'{role}' is not a valid participant role.");
        }

        if (RequiredUserRole[role] != userRole)
        {
            throw new ParticipantRoleMismatchException(userId, userRole, role);
        }

        return new Participant { Deal = deal, UserId = userId, Role = role };
    }
}
