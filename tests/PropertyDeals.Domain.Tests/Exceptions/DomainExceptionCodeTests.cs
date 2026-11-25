using PropertyDeals.Domain.Enums;
using PropertyDeals.Domain.Exceptions;
using PropertyDeals.Domain.Exceptions.Deals;
using PropertyDeals.Domain.Exceptions.Documents;
using PropertyDeals.Domain.Exceptions.Participants;

namespace PropertyDeals.Domain.Tests.Exceptions;

public class DomainExceptionCodeTests
{
    [Fact]
    public void DomainValidationException_HasStableCode() =>
        Assert.Equal("domain.validation", new DomainValidationException("x").Code);

    [Fact]
    public void InvalidDealStatusTransitionException_HasStableCode() =>
        Assert.Equal("deal.invalid_status_transition",
            new InvalidDealStatusTransitionException(DealStatusEnum.Draft, DealStatusEnum.Closed).Code);

    [Fact]
    public void DealNotEditableException_HasStableCode() =>
        Assert.Equal("deal.not_editable", new DealNotEditableException(DealStatusEnum.Closed).Code);

    [Fact]
    public void ClosingDateNotSetException_HasStableCode() =>
        Assert.Equal("deal.closing_date_not_set", new ClosingDateNotSetException(DealStatusEnum.Draft).Code);

    [Fact]
    public void ParticipantAlreadyExistsException_HasStableCode() =>
        Assert.Equal("deal.participant_already_exists", new ParticipantAlreadyExistsException(1).Code);

    [Fact]
    public void ParticipantNotFoundException_HasStableCode() =>
        Assert.Equal("deal.participant_not_found", new ParticipantNotFoundException(1).Code);

    [Fact]
    public void ListingAgentAlreadyAssignedException_HasStableCode() =>
        Assert.Equal("deal.listing_agent_already_assigned", new ListingAgentAlreadyAssignedException().Code);

    [Fact]
    public void CannotRemoveListingAgentException_HasStableCode() =>
        Assert.Equal("deal.cannot_remove_listing_agent", new CannotRemoveListingAgentException().Code);

    [Fact]
    public void ParticipantRoleMismatchException_HasStableCode() =>
        Assert.Equal("deal.participant_role_mismatch",
            new ParticipantRoleMismatchException(1, RolesEnum.Client, ParticipantRoleEnum.ListingAgent).Code);

    [Fact]
    public void InvalidDocumentStatusTransitionException_HasStableCode() =>
        Assert.Equal("document.invalid_status_transition",
            new InvalidDocumentStatusTransitionException(DocumentStatusEnum.Deleted, DocumentStatusEnum.Uploaded).Code);
}
