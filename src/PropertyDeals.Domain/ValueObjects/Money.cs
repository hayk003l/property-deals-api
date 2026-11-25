using PropertyDeals.Domain.Exceptions;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PropertyDeals.Domain.ValueObjects;

public sealed partial record Money
{
    public const int Precision = 18;
    public const int Scale = 2;
    public const int CurrencyLength = 3;

    private const decimal MaxAmount = 9_999_999_999_999_999.99m;

    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string? currency)
    {
        if (amount < 0)
        {
            throw new DomainValidationException("Amount cannot be negative.");
        }

        if (amount > MaxAmount)
        {
            throw new DomainValidationException($"Amount cannot be greater than {MaxAmount.ToString(CultureInfo.InvariantCulture)}.");
        }

        if (decimal.Round(amount, Scale) != amount)
        {
            throw new DomainValidationException($"Amount cannot have more than {Scale} decimal places.");
        }

        var normalizedCurrency = currency?.Trim().ToUpperInvariant();

        if (normalizedCurrency is null || !CurrencyPattern().IsMatch(normalizedCurrency))
        {
            throw new DomainValidationException("Currency must be a three-letter ISO 4217 code, e.g. \"USD\".");
        }

        return new Money(amount, normalizedCurrency);
    }

    public override string ToString() =>
        $"{Amount.ToString("0.00", CultureInfo.InvariantCulture)} {Currency}";

    [GeneratedRegex(@"^[A-Z]{3}$")]
    private static partial Regex CurrencyPattern();
}
