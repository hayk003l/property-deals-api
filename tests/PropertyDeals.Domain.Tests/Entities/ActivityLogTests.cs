using PropertyDeals.Domain.Entities;
using PropertyDeals.Domain.Enums;
using PropertyDeals.Domain.Exceptions;
using static PropertyDeals.Domain.Tests.TestData;

namespace PropertyDeals.Domain.Tests.Entities;

public class ActivityLogTests
{
    [Fact]
    public void Create_WithValidInput_SetsAllProperties()
    {
        var log = ActivityLog.Create(DealId, ActivityActionEnum.DealStatusChanged, ListingAgentId, "Active -> UnderContract", Now);

        Assert.Equal(DealId, log.DealId);
        Assert.Equal(ActivityActionEnum.DealStatusChanged, log.Action);
        Assert.Equal(ListingAgentId, log.UserId);
        Assert.Equal("Active -> UnderContract", log.Details);
        Assert.Equal(Now, log.OccurredOnUtc);
    }

    [Fact]
    public void Create_TrimsDetails()
    {
        var log = ActivityLog.Create(DealId, ActivityActionEnum.DealCreated, ListingAgentId, "  Deal created  ", Now);

        Assert.Equal("Deal created", log.Details);
    }

    [Theory]
    [InlineData(0, ListingAgentId)]
    [InlineData(-1, ListingAgentId)]
    [InlineData(DealId, 0)]
    [InlineData(DealId, -1)]
    public void Create_WithNonPositiveIds_ThrowsDomainValidationException(int dealId, int userId)
    {
        Assert.Throws<DomainValidationException>(
            () => ActivityLog.Create(dealId, ActivityActionEnum.DealCreated, userId, "Deal created", Now));
    }

    [Fact]
    public void Create_WithUndefinedAction_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(
            () => ActivityLog.Create(DealId, (ActivityActionEnum)99, ListingAgentId, "Deal created", Now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingDetails_ThrowsDomainValidationException(string? details)
    {
        Assert.Throws<DomainValidationException>(
            () => ActivityLog.Create(DealId, ActivityActionEnum.DealCreated, ListingAgentId, details!, Now));
    }

    [Fact]
    public void Create_WithDetailsAtMaxLength_Succeeds()
    {
        var details = new string('a', ActivityLog.DetailsMaxLength);

        var log = ActivityLog.Create(DealId, ActivityActionEnum.DealCreated, ListingAgentId, details, Now);

        Assert.Equal(details, log.Details);
    }

    [Fact]
    public void Create_WithDetailsAboveMaxLength_ThrowsDomainValidationException()
    {
        var details = new string('a', ActivityLog.DetailsMaxLength + 1);

        Assert.Throws<DomainValidationException>(
            () => ActivityLog.Create(DealId, ActivityActionEnum.DealCreated, ListingAgentId, details, Now));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Create_WithNonUtcTime_ThrowsArgumentException(DateTimeKind kind)
    {
        Assert.Throws<ArgumentException>(
            () => ActivityLog.Create(DealId, ActivityActionEnum.DealCreated, ListingAgentId, "Deal created", NowAs(kind)));
    }
}
