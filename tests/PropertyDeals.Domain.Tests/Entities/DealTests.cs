using PropertyDeals.Domain.Entities;
using PropertyDeals.Domain.Enums;
using PropertyDeals.Domain.Events.Deals;
using PropertyDeals.Domain.Exceptions;
using PropertyDeals.Domain.Exceptions.Deals;
using PropertyDeals.Domain.Exceptions.Participants;
using static PropertyDeals.Domain.Tests.TestData;

namespace PropertyDeals.Domain.Tests.Entities;

public class DealTests
{
    [Fact]
    public void Create_WithValidInput_StartsInDraftWithoutClosingDate()
    {
        var deal = Deal.Create(PropertyId, ListingAgentId, RolesEnum.Agent, Now);

        Assert.Equal(PropertyId, deal.PropertyId);
        Assert.Equal(DealStatusEnum.Draft, deal.Status);
        Assert.Null(deal.ClosingDate);
        Assert.Equal(ListingAgentId, deal.CreatedBy);
    }

    [Fact]
    public void Create_WithValidInput_AddsCreatorAsListingAgent()
    {
        var deal = Deal.Create(PropertyId, ListingAgentId, RolesEnum.Agent, Now);

        var participant = Assert.Single(deal.DealParticipants);
        Assert.Equal(ListingAgentId, participant.UserId);
        Assert.Equal(ParticipantRoleEnum.ListingAgent, participant.Role);
        Assert.Same(deal, participant.Deal);
    }

    [Fact]
    public void Create_WithValidInput_RaisesDealCreatedEvent()
    {
        var deal = Deal.Create(PropertyId, ListingAgentId, RolesEnum.Agent, Now);

        var raised = Assert.IsType<DealCreatedEvent>(Assert.Single(deal.DomainEvents));
        Assert.Same(deal, raised.Deal);
        Assert.Equal(Now, raised.OccurredOnUtc);
    }

    [Theory]
    [InlineData(0, ListingAgentId)]
    [InlineData(-1, ListingAgentId)]
    [InlineData(PropertyId, 0)]
    [InlineData(PropertyId, -1)]
    public void Create_WithNonPositiveIds_ThrowsDomainValidationException(int propertyId, int listingAgentId)
    {
        Assert.Throws<DomainValidationException>(() => Deal.Create(propertyId, listingAgentId, RolesEnum.Agent, Now));
    }

    [Theory]
    [InlineData(RolesEnum.Client)]
    [InlineData(RolesEnum.Admin)]
    public void Create_WhenListingAgentIsNotAnAgent_ThrowsParticipantRoleMismatchException(RolesEnum userRole)
    {
        var exception = Assert.Throws<ParticipantRoleMismatchException>(
            () => Deal.Create(PropertyId, ListingAgentId, userRole, Now));

        Assert.Equal(ListingAgentId, exception.UserId);
        Assert.Equal(userRole, exception.UserRole);
        Assert.Equal(ParticipantRoleEnum.ListingAgent, exception.ParticipantRole);
    }

    [Fact]
    public void Create_WithUndefinedUserRole_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(() => Deal.Create(PropertyId, ListingAgentId, (RolesEnum)99, Now));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Create_WithNonUtcTime_ThrowsArgumentException(DateTimeKind kind)
    {
        Assert.Throws<ArgumentException>(() => Deal.Create(PropertyId, ListingAgentId, RolesEnum.Agent, NowAs(kind)));
    }

    [Theory]
    [InlineData(DealStatusEnum.Draft, DealStatusEnum.Active)]
    [InlineData(DealStatusEnum.Draft, DealStatusEnum.Cancelled)]
    [InlineData(DealStatusEnum.Active, DealStatusEnum.Cancelled)]
    [InlineData(DealStatusEnum.UnderContract, DealStatusEnum.Closing)]
    [InlineData(DealStatusEnum.UnderContract, DealStatusEnum.Cancelled)]
    [InlineData(DealStatusEnum.Closing, DealStatusEnum.Closed)]
    [InlineData(DealStatusEnum.Closing, DealStatusEnum.Cancelled)]
    public void ChangeStatus_AlongAllowedTransition_UpdatesStatusAndRaisesEvent(DealStatusEnum from, DealStatusEnum to)
    {
        var deal = DealIn(from);

        deal.ChangeStatus(to, ListingAgentId, Now);

        Assert.Equal(to, deal.Status);
        var raised = Assert.IsType<DealStatusChangedEvent>(Assert.Single(deal.DomainEvents));
        Assert.Equal(deal.Id, raised.DealId);
        Assert.Equal(from, raised.OldStatus);
        Assert.Equal(to, raised.NewStatus);
        Assert.Equal(ListingAgentId, raised.ChangedBy);
        Assert.Equal(Now, raised.OccurredOnUtc);
    }

    [Fact]
    public void ChangeStatus_FromActiveToUnderContractWithClosingDate_SetsClosingDateAndRaisesEvent()
    {
        var deal = DealIn(DealStatusEnum.Active);

        deal.ChangeStatus(DealStatusEnum.UnderContract, ListingAgentId, Now, DefaultClosingDate);

        Assert.Equal(DealStatusEnum.UnderContract, deal.Status);
        Assert.Equal(DefaultClosingDate, deal.ClosingDate);
        var raised = Assert.IsType<DealStatusChangedEvent>(Assert.Single(deal.DomainEvents));
        Assert.Equal(DealStatusEnum.Active, raised.OldStatus);
        Assert.Equal(DealStatusEnum.UnderContract, raised.NewStatus);
    }

    [Fact]
    public void ChangeStatus_ToUnderContractWithTodayAsClosingDate_Succeeds()
    {
        var deal = DealIn(DealStatusEnum.Active);

        deal.ChangeStatus(DealStatusEnum.UnderContract, ListingAgentId, Now, Today);

        Assert.Equal(Today, deal.ClosingDate);
    }

    [Fact]
    public void ChangeStatus_AfterUnderContract_KeepsClosingDate()
    {
        var deal = DealIn(DealStatusEnum.UnderContract);

        deal.ChangeStatus(DealStatusEnum.Closing, ListingAgentId, Now);

        Assert.Equal(DefaultClosingDate, deal.ClosingDate);
    }

    [Theory]
    [InlineData(DealStatusEnum.Draft, DealStatusEnum.Draft)]
    [InlineData(DealStatusEnum.Draft, DealStatusEnum.UnderContract)]
    [InlineData(DealStatusEnum.Draft, DealStatusEnum.Closing)]
    [InlineData(DealStatusEnum.Draft, DealStatusEnum.Closed)]
    [InlineData(DealStatusEnum.Active, DealStatusEnum.Draft)]
    [InlineData(DealStatusEnum.Active, DealStatusEnum.Active)]
    [InlineData(DealStatusEnum.Active, DealStatusEnum.Closing)]
    [InlineData(DealStatusEnum.Active, DealStatusEnum.Closed)]
    [InlineData(DealStatusEnum.UnderContract, DealStatusEnum.Draft)]
    [InlineData(DealStatusEnum.UnderContract, DealStatusEnum.Active)]
    [InlineData(DealStatusEnum.UnderContract, DealStatusEnum.Closed)]
    [InlineData(DealStatusEnum.Closing, DealStatusEnum.Active)]
    [InlineData(DealStatusEnum.Closing, DealStatusEnum.UnderContract)]
    [InlineData(DealStatusEnum.Closed, DealStatusEnum.Active)]
    [InlineData(DealStatusEnum.Closed, DealStatusEnum.Closed)]
    [InlineData(DealStatusEnum.Closed, DealStatusEnum.Cancelled)]
    [InlineData(DealStatusEnum.Cancelled, DealStatusEnum.Draft)]
    [InlineData(DealStatusEnum.Cancelled, DealStatusEnum.Active)]
    [InlineData(DealStatusEnum.Cancelled, DealStatusEnum.Cancelled)]
    public void ChangeStatus_AlongForbiddenTransition_ThrowsAndKeepsStatus(DealStatusEnum from, DealStatusEnum to)
    {
        var deal = DealIn(from);

        var exception = Assert.Throws<InvalidDealStatusTransitionException>(
            () => deal.ChangeStatus(to, ListingAgentId, Now));

        Assert.Equal(from, exception.From);
        Assert.Equal(to, exception.To);
        Assert.Equal(from, deal.Status);
        Assert.Empty(deal.DomainEvents);
    }

    [Fact]
    public void ChangeStatus_ToUnderContractWithoutClosingDate_ThrowsAndKeepsStatus()
    {
        var deal = DealIn(DealStatusEnum.Active);

        Assert.Throws<DomainValidationException>(() => deal.ChangeStatus(DealStatusEnum.UnderContract, ListingAgentId, Now));

        Assert.Equal(DealStatusEnum.Active, deal.Status);
        Assert.Null(deal.ClosingDate);
        Assert.Empty(deal.DomainEvents);
    }

    [Fact]
    public void ChangeStatus_ToUnderContractWithPastClosingDate_ThrowsAndKeepsState()
    {
        var deal = DealIn(DealStatusEnum.Active);

        Assert.Throws<DomainValidationException>(
            () => deal.ChangeStatus(DealStatusEnum.UnderContract, ListingAgentId, Now, Today.AddDays(-1)));

        Assert.Equal(DealStatusEnum.Active, deal.Status);
        Assert.Null(deal.ClosingDate);
        Assert.Empty(deal.DomainEvents);
    }

    [Fact]
    public void ChangeStatus_WithClosingDateForStatusOtherThanUnderContract_Throws()
    {
        var deal = DealIn(DealStatusEnum.Draft);

        Assert.Throws<DomainValidationException>(
            () => deal.ChangeStatus(DealStatusEnum.Active, ListingAgentId, Now, DefaultClosingDate));

        Assert.Equal(DealStatusEnum.Draft, deal.Status);
        Assert.Null(deal.ClosingDate);
    }

    [Fact]
    public void ChangeStatus_ToUndefinedStatus_ThrowsDomainValidationException()
    {
        var deal = DealIn(DealStatusEnum.Draft);

        Assert.Throws<DomainValidationException>(() => deal.ChangeStatus((DealStatusEnum)99, ListingAgentId, Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ChangeStatus_WithNonPositiveChangedBy_ThrowsDomainValidationException(int changedBy)
    {
        var deal = DealIn(DealStatusEnum.Draft);

        Assert.Throws<DomainValidationException>(() => deal.ChangeStatus(DealStatusEnum.Active, changedBy, Now));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void ChangeStatus_WithNonUtcTime_ThrowsAndKeepsStatus(DateTimeKind kind)
    {
        var deal = DealIn(DealStatusEnum.Draft);

        Assert.Throws<ArgumentException>(() => deal.ChangeStatus(DealStatusEnum.Active, ListingAgentId, NowAs(kind)));

        Assert.Equal(DealStatusEnum.Draft, deal.Status);
        Assert.Empty(deal.DomainEvents);
    }

    [Theory]
    [InlineData(DealStatusEnum.UnderContract)]
    [InlineData(DealStatusEnum.Closing)]
    public void RescheduleClosing_ToNewDate_UpdatesDateAndRaisesEvent(DealStatusEnum status)
    {
        var deal = DealIn(status);
        var newDate = DefaultClosingDate.AddDays(14);

        deal.RescheduleClosing(newDate, ListingAgentId, Now);

        Assert.Equal(newDate, deal.ClosingDate);
        var raised = Assert.IsType<ClosingDateRescheduledEvent>(Assert.Single(deal.DomainEvents));
        Assert.Equal(deal.Id, raised.DealId);
        Assert.Equal(DefaultClosingDate, raised.OldClosingDate);
        Assert.Equal(newDate, raised.NewClosingDate);
        Assert.Equal(ListingAgentId, raised.ChangedBy);
        Assert.Equal(Now, raised.OccurredOnUtc);
    }

    [Fact]
    public void RescheduleClosing_ToEarlierDateNotInPast_Succeeds()
    {
        var deal = DealIn(DealStatusEnum.UnderContract);

        deal.RescheduleClosing(Today, ListingAgentId, Now);

        Assert.Equal(Today, deal.ClosingDate);
    }

    [Fact]
    public void RescheduleClosing_ToSameDate_DoesNothing()
    {
        var deal = DealIn(DealStatusEnum.UnderContract);

        deal.RescheduleClosing(DefaultClosingDate, ListingAgentId, Now);

        Assert.Equal(DefaultClosingDate, deal.ClosingDate);
        Assert.Empty(deal.DomainEvents);
    }

    [Fact]
    public void RescheduleClosing_ToPastDate_ThrowsAndKeepsDate()
    {
        var deal = DealIn(DealStatusEnum.UnderContract);

        Assert.Throws<DomainValidationException>(() => deal.RescheduleClosing(Today.AddDays(-1), ListingAgentId, Now));

        Assert.Equal(DefaultClosingDate, deal.ClosingDate);
        Assert.Empty(deal.DomainEvents);
    }

    [Theory]
    [InlineData(DealStatusEnum.Draft)]
    [InlineData(DealStatusEnum.Active)]
    public void RescheduleClosing_WithoutClosingDate_ThrowsClosingDateNotSetException(DealStatusEnum status)
    {
        var deal = DealIn(status);

        var exception = Assert.Throws<ClosingDateNotSetException>(
            () => deal.RescheduleClosing(DefaultClosingDate, ListingAgentId, Now));

        Assert.Equal(status, exception.Status);
    }

    [Theory]
    [InlineData(DealStatusEnum.Closed)]
    [InlineData(DealStatusEnum.Cancelled)]
    public void RescheduleClosing_OnFinishedDeal_ThrowsDealNotEditableException(DealStatusEnum status)
    {
        var deal = DealIn(status);

        var exception = Assert.Throws<DealNotEditableException>(
            () => deal.RescheduleClosing(DefaultClosingDate.AddDays(1), ListingAgentId, Now));

        Assert.Equal(status, exception.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void RescheduleClosing_WithNonPositiveChangedBy_ThrowsDomainValidationException(int changedBy)
    {
        var deal = DealIn(DealStatusEnum.UnderContract);

        Assert.Throws<DomainValidationException>(() => deal.RescheduleClosing(DefaultClosingDate.AddDays(1), changedBy, Now));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void RescheduleClosing_WithNonUtcTime_ThrowsAndKeepsDate(DateTimeKind kind)
    {
        var deal = DealIn(DealStatusEnum.UnderContract);

        Assert.Throws<ArgumentException>(
            () => deal.RescheduleClosing(DefaultClosingDate.AddDays(1), ListingAgentId, NowAs(kind)));

        Assert.Equal(DefaultClosingDate, deal.ClosingDate);
    }
}
