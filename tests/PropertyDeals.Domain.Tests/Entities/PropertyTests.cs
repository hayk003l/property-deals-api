using PropertyDeals.Domain.Entities;
using PropertyDeals.Domain.Enums;
using PropertyDeals.Domain.Events.Properties;
using PropertyDeals.Domain.Exceptions;
using static PropertyDeals.Domain.Tests.TestData;

namespace PropertyDeals.Domain.Tests.Entities;

public class PropertyTests
{
    [Fact]
    public void Create_WithValidInput_SetsAllProperties()
    {
        var address = ValidAddress();
        var price = Price();

        var property = Property.Create(address, PropertyTypeEnum.Condo, price, 2, ListingAgentId, Now);

        Assert.Equal(address, property.Address);
        Assert.Equal(PropertyTypeEnum.Condo, property.Type);
        Assert.Equal(price, property.Price);
        Assert.Equal(2, property.Bedrooms);
        Assert.Equal(ListingAgentId, property.ListedById);
    }

    [Fact]
    public void Create_WithValidInput_RaisesPropertyCreatedEvent()
    {
        var property = Property.Create(ValidAddress(), PropertyTypeEnum.House, Price(), 3, ListingAgentId, Now);

        var raised = Assert.IsType<PropertyCreatedEvent>(Assert.Single(property.DomainEvents));
        Assert.Same(property, raised.Property);
        Assert.Equal(Now, raised.OccurredOnUtc);
    }

    [Fact]
    public void Create_WithZeroBedrooms_Succeeds()
    {
        var property = Property.Create(ValidAddress(), PropertyTypeEnum.Commercial, Price(), 0, ListingAgentId, Now);

        Assert.Equal(0, property.Bedrooms);
    }

    [Fact]
    public void Create_WithNegativeBedrooms_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(
            () => Property.Create(ValidAddress(), PropertyTypeEnum.House, Price(), -1, ListingAgentId, Now));
    }

    [Fact]
    public void Create_WithUndefinedType_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(
            () => Property.Create(ValidAddress(), (PropertyTypeEnum)99, Price(), 3, ListingAgentId, Now));
    }

    [Fact]
    public void Create_WithZeroPrice_ThrowsDomainValidationException()
    {
        var exception = Assert.Throws<DomainValidationException>(
            () => Property.Create(ValidAddress(), PropertyTypeEnum.House, Price(0m), 3, ListingAgentId, Now));

        Assert.Contains("positive", exception.Message);
    }

    [Fact]
    public void Create_WithNullPrice_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(
            () => Property.Create(ValidAddress(), PropertyTypeEnum.House, null!, 3, ListingAgentId, Now));
    }

    [Fact]
    public void Create_WithNullAddress_ThrowsDomainValidationException()
    {
        Assert.Throws<DomainValidationException>(
            () => Property.Create(null!, PropertyTypeEnum.House, Price(), 3, ListingAgentId, Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithNonPositiveListedById_ThrowsDomainValidationException(int listedById)
    {
        Assert.Throws<DomainValidationException>(
            () => Property.Create(ValidAddress(), PropertyTypeEnum.House, Price(), 3, listedById, Now));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void Create_WithNonUtcTime_ThrowsArgumentException(DateTimeKind kind)
    {
        Assert.Throws<ArgumentException>(
            () => Property.Create(ValidAddress(), PropertyTypeEnum.House, Price(), 3, ListingAgentId, NowAs(kind)));
    }

    [Fact]
    public void ChangePrice_ToNewAmount_UpdatesPriceAndRaisesEvent()
    {
        var property = NewProperty();
        var oldPrice = property.Price;
        var newPrice = Price(240_000m);

        property.ChangePrice(newPrice, ListingAgentId, Now);

        Assert.Equal(newPrice, property.Price);
        var raised = Assert.IsType<PropertyPriceChangedEvent>(Assert.Single(property.DomainEvents));
        Assert.Equal(property.Id, raised.PropertyId);
        Assert.Equal(oldPrice, raised.OldPrice);
        Assert.Equal(newPrice, raised.NewPrice);
        Assert.Equal(ListingAgentId, raised.ChangedBy);
        Assert.Equal(Now, raised.OccurredOnUtc);
    }

    [Fact]
    public void ChangePrice_ToEqualPrice_DoesNothing()
    {
        var property = NewProperty();

        property.ChangePrice(Price(250_000.00m, "usd"), ListingAgentId, Now);

        Assert.Empty(property.DomainEvents);
    }

    [Fact]
    public void ChangePrice_ToDifferentCurrency_ThrowsAndKeepsPrice()
    {
        var property = NewProperty();
        var oldPrice = property.Price;

        Assert.Throws<DomainValidationException>(() => property.ChangePrice(Price(250_000m, "EUR"), ListingAgentId, Now));

        Assert.Equal(oldPrice, property.Price);
        Assert.Empty(property.DomainEvents);
    }

    [Fact]
    public void ChangePrice_ToZero_ThrowsAndKeepsPrice()
    {
        var property = NewProperty();
        var oldPrice = property.Price;

        Assert.Throws<DomainValidationException>(() => property.ChangePrice(Price(0m), ListingAgentId, Now));

        Assert.Equal(oldPrice, property.Price);
    }

    [Fact]
    public void ChangePrice_ToNull_ThrowsDomainValidationException()
    {
        var property = NewProperty();

        Assert.Throws<DomainValidationException>(() => property.ChangePrice(null!, ListingAgentId, Now));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ChangePrice_WithNonPositiveChangedBy_ThrowsDomainValidationException(int changedBy)
    {
        var property = NewProperty();

        Assert.Throws<DomainValidationException>(() => property.ChangePrice(Price(1m), changedBy, Now));
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void ChangePrice_WithNonUtcTime_ThrowsAndKeepsPrice(DateTimeKind kind)
    {
        var property = NewProperty();
        var oldPrice = property.Price;

        Assert.Throws<ArgumentException>(() => property.ChangePrice(Price(1m), ListingAgentId, NowAs(kind)));

        Assert.Equal(oldPrice, property.Price);
        Assert.Empty(property.DomainEvents);
    }
}
