using System.Globalization;
using PropertyDeals.Domain.Exceptions;
using PropertyDeals.Domain.ValueObjects;

namespace PropertyDeals.Domain.Tests.ValueObjects;

// One test class per production class, in the same folder structure (ValueObjects/Money.cs → ValueObjects/MoneyTests.cs).
// Test names follow Method_Scenario_ExpectedResult, so a failing test tells you what broke without opening it.
public class MoneyTests
{
    // ───────────── Happy path: valid input produces a correct object ─────────────

    // [Fact] = a test with no parameters. It runs once.
    [Fact]
    public void Create_WithValidInput_SetsAmountAndCurrency()
    {
        // Arrange: prepare the inputs.
        var amount = 250_000m;

        // Act: call the code under test. Exactly one action per test.
        var money = Money.Create(amount, "USD");

        // Assert: check the result. Assert.Equal(expected, actual): expected comes FIRST,
        // so the failure message reads "Expected: 250000, Actual: ...".
        Assert.Equal(250_000m, money.Amount);
        Assert.Equal("USD", money.Currency);
    }

    [Fact]
    public void Create_WithZeroAmount_Succeeds()
    {
        // Zero is valid on purpose (see Money). Property adds its own "price > 0" rule.
        var money = Money.Create(0m, "USD");

        Assert.Equal(0m, money.Amount);
    }

    [Fact]
    public void Create_WithMaxAmount_Succeeds()
    {
        // Testing the exact boundary is where bugs hide: is the limit "<" or "<="?
        var money = Money.Create(9_999_999_999_999_999.99m, "USD");

        Assert.Equal(9_999_999_999_999_999.99m, money.Amount);
    }

    // ───────────── Rejected input: each rule has its own test ─────────────

    [Fact]
    public void Create_WithNegativeAmount_ThrowsDomainValidationException()
    {
        // Assert.Throws<T> runs the lambda and passes only if it throws exactly T.
        // The lambda is needed so that xUnit, not the test itself, catches the exception.
        // It also returns the exception, so we can check its message.
        var exception = Assert.Throws<DomainValidationException>(() => Money.Create(-1m, "USD"));

        // Checking the message makes sure the RIGHT rule failed, not just any rule.
        Assert.Contains("negative", exception.Message);
    }

    [Fact]
    public void Create_WithAmountAboveMax_ThrowsDomainValidationException()
    {
        // One cent above the boundary tested in Create_WithMaxAmount_Succeeds.
        var exception = Assert.Throws<DomainValidationException>(
            () => Money.Create(10_000_000_000_000_000.00m, "USD"));

        Assert.Contains("greater than", exception.Message);
    }

    // [Theory] = the same test run once per [InlineData] row. Use it when only the input changes.
    // C# attributes cannot contain decimal values, so the amounts are passed as strings and parsed.
    // InvariantCulture makes "10.555" parse the same way on every machine (in some cultures "," is the decimal separator).
    [Theory]
    [InlineData("10.555")]
    [InlineData("0.001")]
    [InlineData("99.999")]
    public void Create_WithMoreThanTwoDecimals_ThrowsDomainValidationException(string amountText)
    {
        var amount = decimal.Parse(amountText, CultureInfo.InvariantCulture);

        var exception = Assert.Throws<DomainValidationException>(() => Money.Create(amount, "USD"));

        Assert.Contains("decimal places", exception.Message);
    }

    [Theory]
    [InlineData("10")]
    [InlineData("10.5")]
    [InlineData("10.55")]
    public void Create_WithUpToTwoDecimals_Succeeds(string amountText)
    {
        var amount = decimal.Parse(amountText, CultureInfo.InvariantCulture);

        var money = Money.Create(amount, "USD");

        Assert.Equal(amount, money.Amount);
    }

    // Every way a currency can be wrong. null is allowed here because the parameter is string?.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("US")]
    [InlineData("USDD")]
    [InlineData("1$!")]
    [InlineData("U5D")]
    public void Create_WithInvalidCurrency_ThrowsDomainValidationException(string? currency)
    {
        var exception = Assert.Throws<DomainValidationException>(() => Money.Create(10m, currency));

        Assert.Contains("ISO 4217", exception.Message);
    }

    // ───────────── Normalization: input is cleaned up ─────────────

    [Theory]
    [InlineData("USD")]
    [InlineData("usd")]
    [InlineData(" Usd ")]
    public void Create_WithCurrencyInAnyCaseOrSpacing_NormalizesToUpperCase(string currency)
    {
        var money = Money.Create(10m, currency);

        Assert.Equal("USD", money.Currency);
    }

    // ───────────── Equality: value objects are equal when their values are equal ─────────────

    [Fact]
    public void Equals_SameAmountWithDifferentScaleAndCurrencyCase_AreEqual()
    {
        // 10m and 10.00m are stored differently inside decimal, but compare as equal.
        var first = Money.Create(10m, "USD");
        var second = Money.Create(10.00m, "usd");

        Assert.Equal(first, second);
    }

    [Fact]
    public void Equals_SameAmountWithDifferentCurrency_AreNotEqual()
    {
        var dollars = Money.Create(10m, "USD");
        var euros = Money.Create(10m, "EUR");

        Assert.NotEqual(dollars, euros);
    }

    // ───────────── ToString ─────────────

    [Fact]
    public void ToString_Always_FormatsWithTwoDecimalsAndCurrency()
    {
        var money = Money.Create(250_000m, "USD");

        Assert.Equal("250000.00 USD", money.ToString());
    }

    [Fact]
    public void ToString_UnderCultureWithCommaDecimalSeparator_StillUsesDot()
    {
        // German formats 10.5 as "10,50". Money.ToString uses InvariantCulture, so it must not.
        // We change the culture for this test only and ALWAYS restore it in finally,
        // so other tests are not affected even if this one fails.
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("de-DE");

            var money = Money.Create(10.5m, "EUR");

            Assert.Equal("10.50 EUR", money.ToString());
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }
}
