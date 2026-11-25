using PropertyDeals.Domain.Entities.Base;
using PropertyDeals.Domain.Enums;
using PropertyDeals.Domain.Events.Deals;
using PropertyDeals.Domain.Events.Participants;
using PropertyDeals.Domain.Exceptions;
using PropertyDeals.Domain.Exceptions.Deals;
using PropertyDeals.Domain.Exceptions.Participants;

namespace PropertyDeals.Domain.Entities;

public class Deal : AggregateRoot
{
    private readonly List<Participant> _dealParticipants = [];

    public int PropertyId { get; private set; }
    public DealStatusEnum Status { get; private set; }
    public DateOnly? ClosingDate { get; private set; }
    public int CreatedBy { get; private set; }

    public IReadOnlyCollection<Participant> DealParticipants => _dealParticipants.AsReadOnly();

    private static readonly Dictionary<DealStatusEnum, DealStatusEnum[]> AllowedStatusTransitions = new()
    {
        { DealStatusEnum.Draft, new[] { DealStatusEnum.Active, DealStatusEnum.Cancelled } },
        { DealStatusEnum.Active, new[] { DealStatusEnum.UnderContract, DealStatusEnum.Cancelled } },
        { DealStatusEnum.UnderContract, new[] { DealStatusEnum.Closing, DealStatusEnum.Cancelled } },
        { DealStatusEnum.Closing, new[] { DealStatusEnum.Closed, DealStatusEnum.Cancelled } },
        { DealStatusEnum.Closed, Array.Empty<DealStatusEnum>() },
        { DealStatusEnum.Cancelled, Array.Empty<DealStatusEnum>() }
    };

    private Deal() { }

    public static Deal Create(int propertyId, int listingAgentId, RolesEnum listingAgentUserRole, DateTime utcNow)
    {
        Guard.EnsureUtc(utcNow);

        if (propertyId <= 0 || listingAgentId <= 0)
        {
            throw new DomainValidationException("Property id and listing agent id must be positive.");
        }

        var deal = new Deal { PropertyId = propertyId, Status = DealStatusEnum.Draft, CreatedBy = listingAgentId };
        deal._dealParticipants.Add(Participant.Create(deal, listingAgentId, listingAgentUserRole, ParticipantRoleEnum.ListingAgent));
        deal.RaiseDomainEvent(new DealCreatedEvent(deal, utcNow));
        return deal;
    }

    public void ChangeStatus(DealStatusEnum newStatus, int changedBy, DateTime utcNow, DateOnly? closingDate = null)
    {
        Guard.EnsureUtc(utcNow);

        if (!Enum.IsDefined(newStatus))
        {
            throw new DomainValidationException($"'{newStatus}' is not a valid deal status.");
        }

        if (changedBy <= 0)
        {
            throw new DomainValidationException("Changed-by user id must be positive.");
        }

        if (!CanTransitionTo(newStatus))
        {
            throw new InvalidDealStatusTransitionException(Status, newStatus);
        }

        if (newStatus == DealStatusEnum.UnderContract)
        {
            if (closingDate is null)
            {
                throw new DomainValidationException("A closing date is required when a deal goes under contract.");
            }

            EnsureNotInPast(closingDate.Value, utcNow);
            ClosingDate = closingDate.Value;
        }
        else if (closingDate is not null)
        {
            throw new DomainValidationException($"A closing date can only be set when moving to {DealStatusEnum.UnderContract}.");
        }

        var oldStatus = Status;
        Status = newStatus;
        RaiseDomainEvent(new DealStatusChangedEvent(Id, oldStatus, newStatus, changedBy, utcNow));
    }

    public void RescheduleClosing(DateOnly newClosingDate, int changedBy, DateTime utcNow)
    {
        Guard.EnsureUtc(utcNow);

        if (changedBy <= 0)
        {
            throw new DomainValidationException("Changed-by user id must be positive.");
        }

        EnsureEditable();

        if (ClosingDate is null)
        {
            throw new ClosingDateNotSetException(Status);
        }

        EnsureNotInPast(newClosingDate, utcNow);

        if (newClosingDate == ClosingDate)
        {
            return;
        }

        var oldClosingDate = ClosingDate.Value;
        ClosingDate = newClosingDate;
        RaiseDomainEvent(new ClosingDateRescheduledEvent(Id, oldClosingDate, newClosingDate, changedBy, utcNow));
    }

    public void AddParticipant(int userId, RolesEnum userRole, ParticipantRoleEnum role, int addedBy, DateTime utcNow)
    {
        Guard.EnsureUtc(utcNow);

        if (userId <= 0 || addedBy <= 0)
        {
            throw new DomainValidationException("User id and added-by user id must be positive.");
        }

        if (!Enum.IsDefined(role))
        {
            throw new DomainValidationException($"'{role}' is not a valid participant role.");
        }

        EnsureEditable();

        if (IsParticipant(userId))
        {
            throw new ParticipantAlreadyExistsException(userId);
        }

        if (role == ParticipantRoleEnum.ListingAgent && DealParticipants.Any(d => d.Role == ParticipantRoleEnum.ListingAgent))
        {
            throw new ListingAgentAlreadyAssignedException();
        }

        _dealParticipants.Add(Participant.Create(this, userId, userRole, role));
        RaiseDomainEvent(new ParticipantAddedEvent(Id, userId, role, addedBy, utcNow));
    }

    public void RemoveParticipant(int userId, int removedBy, DateTime utcNow)
    {
        Guard.EnsureUtc(utcNow);

        if (userId <= 0 || removedBy <= 0)
        {
            throw new DomainValidationException("User id and removed-by user id must be positive.");
        }

        EnsureEditable();

        if (!IsParticipant(userId))
        {
            throw new ParticipantNotFoundException(userId);
        }

        if (DealParticipants.Any(d => d.UserId == userId && d.Role == ParticipantRoleEnum.ListingAgent))
        {
            throw new CannotRemoveListingAgentException();
        }

        var dealParticipant = _dealParticipants.First(d => d.UserId == userId);
        _dealParticipants.Remove(dealParticipant);
        RaiseDomainEvent(new ParticipantRemovedEvent(Id, userId, dealParticipant.Role, removedBy, utcNow));
    }

    public bool IsParticipant(int userId) =>
        DealParticipants.Any(d => d.UserId == userId);

    private bool CanTransitionTo(DealStatusEnum newStatus) =>
        AllowedStatusTransitions.TryGetValue(Status, out var allowed) && allowed.Contains(newStatus);

    private void EnsureEditable()
    {
        if (Status == DealStatusEnum.Cancelled || Status == DealStatusEnum.Closed)
        {
            throw new DealNotEditableException(Status);
        }
    }

    private static void EnsureNotInPast(DateOnly closingDate, DateTime utcNow)
    {
        if (closingDate < DateOnly.FromDateTime(utcNow))
        {
            throw new DomainValidationException("The closing date cannot be in the past.");
        }
    }
}
