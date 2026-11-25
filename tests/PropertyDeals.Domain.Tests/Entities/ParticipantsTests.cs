using PropertyDeals.Domain.Enums;
using PropertyDeals.Domain.Events.Participants;
using PropertyDeals.Domain.Exceptions;
using PropertyDeals.Domain.Exceptions.Deals;
using PropertyDeals.Domain.Exceptions.Participants;
using static PropertyDeals.Domain.Tests.TestData;

namespace PropertyDeals.Domain.Tests.Entities;

public class ParticipantsTests
{
    [Theory]
    [InlineData(BuyerAgentId, RolesEnum.Agent, ParticipantRoleEnum.BuyerAgent)]
    [InlineData(BuyerId, RolesEnum.Client, ParticipantRoleEnum.Buyer)]
    [InlineData(SellerId, RolesEnum.Client, ParticipantRoleEnum.Seller)]
    public void AddParticipant_WithMatchingUserRole_AddsParticipantAndRaisesEvent(
        int userId, RolesEnum userRole, ParticipantRoleEnum role)
    {
        var deal = NewDeal();

        deal.AddParticipant(userId, userRole, role, ListingAgentId, Now);

        Assert.Equal(2, deal.DealParticipants.Count);
        var participant = Assert.Single(deal.DealParticipants, p => p.UserId == userId);
        Assert.Equal(role, participant.Role);
        Assert.Same(deal, participant.Deal);

        var raised = Assert.IsType<ParticipantAddedEvent>(Assert.Single(deal.DomainEvents));
        Assert.Equal(deal.Id, raised.DealId);
        Assert.Equal(userId, raised.UserId);
        Assert.Equal(role, raised.Role);
        Assert.Equal(ListingAgentId, raised.AddedBy);
        Assert.Equal(Now, raised.OccurredOnUtc);
    }

    [Theory]
    [InlineData(DealStatusEnum.Active)]
    [InlineData(DealStatusEnum.UnderContract)]
    [InlineData(DealStatusEnum.Closing)]
    public void AddParticipant_OnOpenDeal_Succeeds(DealStatusEnum status)
    {
        var deal = DealIn(status);

        deal.AddParticipant(BuyerId, RolesEnum.Client, ParticipantRoleEnum.Buyer, ListingAgentId, Now);

        Assert.True(deal.IsParticipant(BuyerId));
    }

    [Theory]
    [InlineData(RolesEnum.Client, ParticipantRoleEnum.BuyerAgent)]
    [InlineData(RolesEnum.Admin, ParticipantRoleEnum.BuyerAgent)]
    [InlineData(RolesEnum.Agent, ParticipantRoleEnum.Buyer)]
    [InlineData(RolesEnum.Admin, ParticipantRoleEnum.Buyer)]
    [InlineData(RolesEnum.Agent, ParticipantRoleEnum.Seller)]
    [InlineData(RolesEnum.Admin, ParticipantRoleEnum.Seller)]
    public void AddParticipant_WithMismatchedUserRole_ThrowsAndAddsNothing(RolesEnum userRole, ParticipantRoleEnum role)
    {
        var deal = NewDeal();

        var exception = Assert.Throws<ParticipantRoleMismatchException>(
            () => deal.AddParticipant(StrangerId, userRole, role, ListingAgentId, Now));

        Assert.Equal(StrangerId, exception.UserId);
        Assert.Equal(userRole, exception.UserRole);
        Assert.Equal(role, exception.ParticipantRole);
        Assert.Single(deal.DealParticipants);
        Assert.Empty(deal.DomainEvents);
    }

    [Fact]
    public void AddParticipant_SameUserTwice_ThrowsParticipantAlreadyExistsException()
    {
        var deal = NewDeal();
        deal.AddParticipant(BuyerId, RolesEnum.Client, ParticipantRoleEnum.Buyer, ListingAgentId, Now);

        var exception = Assert.Throws<ParticipantAlreadyExistsException>(
            () => deal.AddParticipant(BuyerId, RolesEnum.Client, ParticipantRoleEnum.Seller, ListingAgentId, Now));

        Assert.Equal(BuyerId, exception.UserId);
        Assert.Equal(2, deal.DealParticipants.Count);
    }

    [Fact]
    public void AddParticipant_SecondListingAgent_ThrowsListingAgentAlreadyAssignedException()
    {
        var deal = NewDeal();

        Assert.Throws<ListingAgentAlreadyAssignedException>(
            () => deal.AddParticipant(StrangerId, RolesEnum.Agent, ParticipantRoleEnum.ListingAgent, ListingAgentId, Now));

        Assert.Single(deal.DealParticipants);
    }

    [Theory]
    [InlineData(DealStatusEnum.Closed)]
    [InlineData(DealStatusEnum.Cancelled)]
    public void AddParticipant_OnFinishedDeal_ThrowsDealNotEditableException(DealStatusEnum status)
    {
        var deal = DealIn(status);

        var exception = Assert.Throws<DealNotEditableException>(
            () => deal.AddParticipant(BuyerId, RolesEnum.Client, ParticipantRoleEnum.Buyer, ListingAgentId, Now));

        Assert.Equal(status, exception.Status);
        Assert.False(deal.IsParticipant(BuyerId));
    }

    [Fact]
    public void AddParticipant_ExistingUserOnFinishedDeal_ThrowsDealNotEditableException()
    {
        var deal = DealIn(DealStatusEnum.Closed);

        Assert.Throws<DealNotEditableException>(
            () => deal.AddParticipant(ListingAgentId, RolesEnum.Agent, ParticipantRoleEnum.BuyerAgent, ListingAgentId, Now));
    }

    [Fact]
    public void AddParticipant_WithUndefinedRole_ThrowsDomainValidationException()
    {
        var deal = NewDeal();

        Assert.Throws<DomainValidationException>(
            () => deal.AddParticipant(BuyerId, RolesEnum.Client, (ParticipantRoleEnum)99, ListingAgentId, Now));
    }

    [Fact]
    public void AddParticipant_WithUndefinedUserRole_ThrowsDomainValidationException()
    {
        var deal = NewDeal();

        Assert.Throws<DomainValidationException>(
            () => deal.AddParticipant(BuyerId, (RolesEnum)99, ParticipantRoleEnum.Buyer, ListingAgentId, Now));
    }

    [Theory]
    [InlineData(0, ListingAgentId)]
    [InlineData(-1, ListingAgentId)]
    [InlineData(BuyerId, 0)]
    [InlineData(BuyerId, -1)]
    public void AddParticipant_WithNonPositiveIds_ThrowsDomainValidationException(int userId, int addedBy)
    {
        var deal = NewDeal();

        Assert.Throws<DomainValidationException>(
            () => deal.AddParticipant(userId, RolesEnum.Client, ParticipantRoleEnum.Buyer, addedBy, Now));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void AddParticipant_WithNonUtcTime_ThrowsAndAddsNothing(DateTimeKind kind)
    {
        var deal = NewDeal();

        Assert.Throws<ArgumentException>(
            () => deal.AddParticipant(BuyerId, RolesEnum.Client, ParticipantRoleEnum.Buyer, ListingAgentId, NowAs(kind)));

        Assert.False(deal.IsParticipant(BuyerId));
    }

    [Fact]
    public void RemoveParticipant_ExistingParticipant_RemovesAndRaisesEvent()
    {
        var deal = NewDeal();
        deal.AddParticipant(BuyerId, RolesEnum.Client, ParticipantRoleEnum.Buyer, ListingAgentId, Now);
        deal.ClearDomainEvents();

        deal.RemoveParticipant(BuyerId, ListingAgentId, Now);

        Assert.False(deal.IsParticipant(BuyerId));
        Assert.Single(deal.DealParticipants);
        var raised = Assert.IsType<ParticipantRemovedEvent>(Assert.Single(deal.DomainEvents));
        Assert.Equal(deal.Id, raised.DealId);
        Assert.Equal(BuyerId, raised.UserId);
        Assert.Equal(ParticipantRoleEnum.Buyer, raised.Role);
        Assert.Equal(ListingAgentId, raised.RemovedBy);
        Assert.Equal(Now, raised.OccurredOnUtc);
    }

    [Fact]
    public void RemoveParticipant_UnknownUser_ThrowsParticipantNotFoundException()
    {
        var deal = NewDeal();

        var exception = Assert.Throws<ParticipantNotFoundException>(
            () => deal.RemoveParticipant(StrangerId, ListingAgentId, Now));

        Assert.Equal(StrangerId, exception.UserId);
    }

    [Fact]
    public void RemoveParticipant_ListingAgent_ThrowsCannotRemoveListingAgentException()
    {
        var deal = NewDeal();

        Assert.Throws<CannotRemoveListingAgentException>(() => deal.RemoveParticipant(ListingAgentId, ListingAgentId, Now));

        Assert.True(deal.IsParticipant(ListingAgentId));
    }

    [Theory]
    [InlineData(DealStatusEnum.Closed)]
    [InlineData(DealStatusEnum.Cancelled)]
    public void RemoveParticipant_OnFinishedDeal_ThrowsDealNotEditableException(DealStatusEnum status)
    {
        var deal = NewDeal();
        deal.AddParticipant(BuyerId, RolesEnum.Client, ParticipantRoleEnum.Buyer, ListingAgentId, Now);
        MoveTo(deal, status);

        var exception = Assert.Throws<DealNotEditableException>(() => deal.RemoveParticipant(BuyerId, ListingAgentId, Now));

        Assert.Equal(status, exception.Status);
        Assert.True(deal.IsParticipant(BuyerId));
    }

    [Fact]
    public void RemoveParticipant_UnknownUserOnFinishedDeal_ThrowsDealNotEditableException()
    {
        var deal = DealIn(DealStatusEnum.Cancelled);

        Assert.Throws<DealNotEditableException>(() => deal.RemoveParticipant(StrangerId, ListingAgentId, Now));
    }

    [Theory]
    [InlineData(0, ListingAgentId)]
    [InlineData(-1, ListingAgentId)]
    [InlineData(BuyerId, 0)]
    [InlineData(BuyerId, -1)]
    public void RemoveParticipant_WithNonPositiveIds_ThrowsDomainValidationException(int userId, int removedBy)
    {
        var deal = NewDeal();

        Assert.Throws<DomainValidationException>(() => deal.RemoveParticipant(userId, removedBy, Now));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void RemoveParticipant_WithNonUtcTime_ThrowsAndKeepsParticipant(DateTimeKind kind)
    {
        var deal = NewDeal();
        deal.AddParticipant(BuyerId, RolesEnum.Client, ParticipantRoleEnum.Buyer, ListingAgentId, Now);

        Assert.Throws<ArgumentException>(() => deal.RemoveParticipant(BuyerId, ListingAgentId, NowAs(kind)));

        Assert.True(deal.IsParticipant(BuyerId));
    }

    [Fact]
    public void IsParticipant_ForListingAgent_ReturnsTrue()
    {
        var deal = NewDeal();

        Assert.True(deal.IsParticipant(ListingAgentId));
    }

    [Fact]
    public void IsParticipant_ForUnknownUser_ReturnsFalse()
    {
        var deal = NewDeal();

        Assert.False(deal.IsParticipant(StrangerId));
    }
}
