using System.Runtime.CompilerServices;

namespace PropertyDeals.Domain;

internal static class Guard
{
    public static void EnsureUtc(DateTime value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException($"{paramName} must be a UTC time, but its Kind is {value.Kind}.", paramName);
        }
    }
}
