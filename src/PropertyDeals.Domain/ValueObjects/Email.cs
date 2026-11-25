using PropertyDeals.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace PropertyDeals.Domain.ValueObjects;

/// <summary>
/// An email address, stored trimmed and in lower case so that uniqueness checks
/// and comparisons are case-insensitive. The only way to get one is <see cref="Create"/>.
/// </summary>
public sealed partial record Email
{
    public const int MaxLength = 254;

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainValidationException("Email is required.");
        }

        var normalized = value.Trim().ToLowerInvariant();

        if (normalized.Length > MaxLength)
        {
            throw new DomainValidationException($"Email cannot be longer than {MaxLength} characters.");
        }

        if (!EmailPattern().IsMatch(normalized))
        {
            throw new DomainValidationException("Email format is invalid.");
        }

        return new Email(normalized);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailPattern();
}
