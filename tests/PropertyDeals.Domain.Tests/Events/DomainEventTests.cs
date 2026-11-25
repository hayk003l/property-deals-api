using PropertyDeals.Domain.Enums;
using PropertyDeals.Domain.Events.Deals;
using static PropertyDeals.Domain.Tests.TestData;

namespace PropertyDeals.Domain.Tests.Events;

public class DomainEventTests
{
    private static DealStatusChangedEvent NewEvent(DateTime occurredOnUtc) =>
        new(DealId, DealStatusEnum.Draft, DealStatusEnum.Active, ListingAgentId, occurredOnUtc);

    [Fact]
    public void Constructor_WithUtcTime_SetsOccurredOnUtc()
    {
        var domainEvent = NewEvent(Now);

        Assert.Equal(Now, domainEvent.OccurredOnUtc);
    }

    [Fact]
    public void Constructor_AssignsNonEmptyId()
    {
        var domainEvent = NewEvent(Now);

        Assert.NotEqual(Guid.Empty, domainEvent.Id);
    }

    [Fact]
    public void Constructor_TwoEventsWithSameData_GetDifferentIds()
    {
        var first = NewEvent(Now);
        var second = NewEvent(Now);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Id_ReadTwice_ReturnsSameValue()
    {
        var domainEvent = NewEvent(Now);

        Assert.Equal(domainEvent.Id, domainEvent.Id);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Constructor_WithNonUtcTime_ThrowsArgumentException(DateTimeKind kind)
    {
        var exception = Assert.Throws<ArgumentException>(() => NewEvent(NowAs(kind)));

        Assert.Equal("occurredOnUtc", exception.ParamName);
    }
}
