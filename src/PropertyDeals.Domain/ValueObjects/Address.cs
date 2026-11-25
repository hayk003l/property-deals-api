using PropertyDeals.Domain.Exceptions;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace PropertyDeals.Domain.ValueObjects;

public sealed partial record Address
{
    public const int StreetMaxLength = 200;
    public const int CityMaxLength = 100;
    public const int StateMaxLength = 100;
    public const int ZipCodeMaxLength = 10;
    public const int CountryLength = 2;

    public string Street { get; }
    public string City { get; }
    public string? State { get; }
    public string ZipCode { get; }
    public string Country { get; }

    private Address(string street, string city, string? state, string zipCode, string country)
    {
        Street = street;
        City = city;
        State = state;
        ZipCode = zipCode;
        Country = country;
    }

    // Parameters are nullable because callers (API input) may pass null; Create rejects it.
    public static Address Create(string? street, string? city, string? state, string? zipCode, string? country)
    {
        var normalizedStreet = Normalize(street);
        var normalizedCity = Normalize(city);
        var normalizedState = Normalize(state);
        var normalizedZipCode = Normalize(zipCode)?.ToUpperInvariant();
        var normalizedCountry = Normalize(country)?.ToUpperInvariant();

        EnsureRequired(normalizedStreet, "Street", StreetMaxLength);
        EnsureRequired(normalizedCity, "City", CityMaxLength);
        EnsureRequired(normalizedZipCode, "Zip code", ZipCodeMaxLength);

        if (normalizedState is not null && normalizedState.Length > StateMaxLength)
        {
            throw new DomainValidationException($"State cannot be longer than {StateMaxLength} characters.");
        }

        if (!ZipCodePattern().IsMatch(normalizedZipCode))
        {
            throw new DomainValidationException("Zip code may contain only letters, digits, spaces and hyphens.");
        }

        if (normalizedCountry is null || !CountryPattern().IsMatch(normalizedCountry))
        {
            throw new DomainValidationException("Country must be a two-letter ISO 3166-1 code, e.g. \"US\".");
        }

        return new Address(normalizedStreet, normalizedCity, normalizedState, normalizedZipCode, normalizedCountry);
    }

    public override string ToString() =>
        State is null
            ? $"{Street}, {City} {ZipCode}, {Country}"
            : $"{Street}, {City}, {State} {ZipCode}, {Country}";

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : WhitespacePattern().Replace(value.Trim(), " ");

    // [NotNull] tells the compiler that value is non-null after this method returns
    // (it throws otherwise), so callers need no extra null checks or '!'.
    private static void EnsureRequired([NotNull] string? value, string name, int maxLength)
    {
        if (value is null)
        {
            throw new DomainValidationException($"{name} is required.");
        }

        if (value.Length > maxLength)
        {
            throw new DomainValidationException($"{name} cannot be longer than {maxLength} characters.");
        }
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();

    [GeneratedRegex(@"^[A-Z0-9][A-Z0-9 \-]*$")]
    private static partial Regex ZipCodePattern();

    [GeneratedRegex(@"^[A-Z]{2}$")]
    private static partial Regex CountryPattern();
}
