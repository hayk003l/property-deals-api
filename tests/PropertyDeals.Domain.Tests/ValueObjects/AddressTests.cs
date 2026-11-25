using PropertyDeals.Domain.Exceptions;
using PropertyDeals.Domain.ValueObjects;

namespace PropertyDeals.Domain.Tests.ValueObjects;

public class AddressTests
{
    private const string Street = "1 Main St";
    private const string City = "Springfield";
    private const string State = "IL";
    private const string ZipCode = "62701";
    private const string Country = "US";

    [Fact]
    public void Create_WithValidInput_SetsAllProperties()
    {
        var address = Address.Create(Street, City, State, ZipCode, Country);

        Assert.Equal(Street, address.Street);
        Assert.Equal(City, address.City);
        Assert.Equal(State, address.State);
        Assert.Equal(ZipCode, address.ZipCode);
        Assert.Equal(Country, address.Country);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithoutState_SetsStateToNull(string? state)
    {
        var address = Address.Create(Street, City, state, ZipCode, Country);

        Assert.Null(address.State);
    }

    [Fact]
    public void Create_TrimsAndCollapsesWhitespace()
    {
        var address = Address.Create("  1   Main \t St  ", "  New   York ", "  New  York  ", " 10001 ", " US ");

        Assert.Equal("1 Main St", address.Street);
        Assert.Equal("New York", address.City);
        Assert.Equal("New York", address.State);
        Assert.Equal("10001", address.ZipCode);
        Assert.Equal("US", address.Country);
    }

    [Fact]
    public void Create_UpperCasesZipCodeAndCountry()
    {
        var address = Address.Create("10 Downing St", "London", null, "sw1a 2aa", "gb");

        Assert.Equal("SW1A 2AA", address.ZipCode);
        Assert.Equal("GB", address.Country);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingStreet_ThrowsDomainValidationException(string? street)
    {
        var exception = Assert.Throws<DomainValidationException>(() => Address.Create(street, City, State, ZipCode, Country));

        Assert.Contains("Street is required", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingCity_ThrowsDomainValidationException(string? city)
    {
        var exception = Assert.Throws<DomainValidationException>(() => Address.Create(Street, city, State, ZipCode, Country));

        Assert.Contains("City is required", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingZipCode_ThrowsDomainValidationException(string? zipCode)
    {
        var exception = Assert.Throws<DomainValidationException>(() => Address.Create(Street, City, State, zipCode, Country));

        Assert.Contains("Zip code is required", exception.Message);
    }

    [Fact]
    public void Create_WithFieldsAtMaxLength_Succeeds()
    {
        var address = Address.Create(
            new string('s', Address.StreetMaxLength),
            new string('c', Address.CityMaxLength),
            new string('t', Address.StateMaxLength),
            new string('1', Address.ZipCodeMaxLength),
            Country);

        Assert.Equal(Address.StreetMaxLength, address.Street.Length);
        Assert.Equal(Address.CityMaxLength, address.City.Length);
        Assert.Equal(Address.StateMaxLength, address.State!.Length);
        Assert.Equal(Address.ZipCodeMaxLength, address.ZipCode.Length);
    }

    [Fact]
    public void Create_WithStreetAboveMaxLength_ThrowsDomainValidationException()
    {
        var street = new string('s', Address.StreetMaxLength + 1);

        Assert.Throws<DomainValidationException>(() => Address.Create(street, City, State, ZipCode, Country));
    }

    [Fact]
    public void Create_WithCityAboveMaxLength_ThrowsDomainValidationException()
    {
        var city = new string('c', Address.CityMaxLength + 1);

        Assert.Throws<DomainValidationException>(() => Address.Create(Street, city, State, ZipCode, Country));
    }

    [Fact]
    public void Create_WithStateAboveMaxLength_ThrowsDomainValidationException()
    {
        var state = new string('t', Address.StateMaxLength + 1);

        Assert.Throws<DomainValidationException>(() => Address.Create(Street, City, state, ZipCode, Country));
    }

    [Fact]
    public void Create_WithZipCodeAboveMaxLength_ThrowsDomainValidationException()
    {
        var zipCode = new string('1', Address.ZipCodeMaxLength + 1);

        Assert.Throws<DomainValidationException>(() => Address.Create(Street, City, State, zipCode, Country));
    }

    [Theory]
    [InlineData("12345")]
    [InlineData("12345-6789")]
    [InlineData("SW1A 2AA")]
    [InlineData("0025")]
    public void Create_WithValidZipCodeFormats_Succeeds(string zipCode)
    {
        var address = Address.Create(Street, City, State, zipCode, Country);

        Assert.Equal(zipCode, address.ZipCode);
    }

    [Theory]
    [InlineData("-12345")]
    [InlineData("12_345")]
    [InlineData("12345!")]
    [InlineData("#123")]
    public void Create_WithInvalidZipCode_ThrowsDomainValidationException(string zipCode)
    {
        var exception = Assert.Throws<DomainValidationException>(() => Address.Create(Street, City, State, zipCode, Country));

        Assert.Contains("Zip code may contain only", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("U")]
    [InlineData("USA")]
    [InlineData("U1")]
    [InlineData("U S")]
    public void Create_WithInvalidCountry_ThrowsDomainValidationException(string? country)
    {
        var exception = Assert.Throws<DomainValidationException>(() => Address.Create(Street, City, State, ZipCode, country));

        Assert.Contains("ISO 3166-1", exception.Message);
    }

    [Fact]
    public void ToString_WithState_IncludesState()
    {
        var address = Address.Create(Street, City, State, ZipCode, Country);

        Assert.Equal("1 Main St, Springfield, IL 62701, US", address.ToString());
    }

    [Fact]
    public void ToString_WithoutState_SkipsState()
    {
        var address = Address.Create("10 Downing St", "London", null, "SW1A 2AA", "GB");

        Assert.Equal("10 Downing St, London SW1A 2AA, GB", address.ToString());
    }

    [Fact]
    public void Equals_SameValuesWithDifferentCaseAndSpacing_AreEqual()
    {
        var first = Address.Create(Street, City, State, ZipCode, Country);
        var second = Address.Create("  1  Main St ", City, State, " 62701 ", "us");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Equals_DifferentValues_AreNotEqual()
    {
        var first = Address.Create(Street, City, State, ZipCode, Country);
        var second = Address.Create("2 Main St", City, State, ZipCode, Country);

        Assert.NotEqual(first, second);
    }
}
