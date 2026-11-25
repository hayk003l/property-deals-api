using PropertyDeals.Domain.Exceptions;
using PropertyDeals.Domain.ValueObjects;

namespace PropertyDeals.Domain.Tests.ValueObjects;

public class EmailTests
{
    private const string Domain = "@example.com";

    [Fact]
    public void Create_WithValidAddress_SetsValue()
    {
        var email = Email.Create("john@example.com");

        Assert.Equal("john@example.com", email.Value);
    }

    [Theory]
    [InlineData("John@Example.COM")]
    [InlineData("  john@example.com  ")]
    [InlineData(" JOHN@EXAMPLE.COM ")]
    public void Create_WithAnyCaseOrSpacing_NormalizesToTrimmedLowerCase(string value)
    {
        var email = Email.Create(value);

        Assert.Equal("john@example.com", email.Value);
    }

    [Theory]
    [InlineData("first.last@example.com")]
    [InlineData("first+tag@example.co.uk")]
    [InlineData("a@b.io")]
    public void Create_WithCommonValidFormats_Succeeds(string value)
    {
        var email = Email.Create(value);

        Assert.Equal(value, email.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithMissingValue_ThrowsDomainValidationException(string? value)
    {
        var exception = Assert.Throws<DomainValidationException>(() => Email.Create(value));

        Assert.Contains("required", exception.Message);
    }

    [Theory]
    [InlineData("plainaddress")]
    [InlineData("@example.com")]
    [InlineData("john@")]
    [InlineData("john@example")]
    [InlineData("john@@example.com")]
    [InlineData("john@exa@mple.com")]
    [InlineData("jo hn@example.com")]
    [InlineData("john@exa mple.com")]
    public void Create_WithInvalidFormat_ThrowsDomainValidationException(string value)
    {
        var exception = Assert.Throws<DomainValidationException>(() => Email.Create(value));

        Assert.Contains("format", exception.Message);
    }

    [Fact]
    public void Create_AtMaxLength_Succeeds()
    {
        var value = new string('a', Email.MaxLength - Domain.Length) + Domain;

        var email = Email.Create(value);

        Assert.Equal(Email.MaxLength, email.Value.Length);
    }

    [Fact]
    public void Create_AboveMaxLength_ThrowsDomainValidationException()
    {
        var value = new string('a', Email.MaxLength - Domain.Length + 1) + Domain;

        var exception = Assert.Throws<DomainValidationException>(() => Email.Create(value));

        Assert.Contains("longer than", exception.Message);
    }

    [Fact]
    public void Equals_SameAddressInDifferentCase_AreEqual()
    {
        Assert.Equal(Email.Create("John@Example.com"), Email.Create("john@example.com"));
    }

    [Fact]
    public void Equals_DifferentAddresses_AreNotEqual()
    {
        Assert.NotEqual(Email.Create("john@example.com"), Email.Create("jane@example.com"));
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        Assert.Equal("john@example.com", Email.Create("John@Example.com").ToString());
    }
}
