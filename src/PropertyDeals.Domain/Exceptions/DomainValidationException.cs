namespace PropertyDeals.Domain.Exceptions;

/// <summary>An input value breaks a domain rule (e.g. a non-positive id or an undefined enum value).</summary>
public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string message)
        : base("domain.validation", message)
    {
    }
}
