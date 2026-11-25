namespace PropertyDeals.Domain.Exceptions;

/// <summary>
/// Base type for every business-rule violation in the domain.
/// <see cref="Code"/> is a stable, machine-readable error code returned in ProblemDetails.
/// </summary>
public abstract class DomainException : Exception
{
    public string Code { get; }

    protected DomainException(string code, string message) : base(message)
    {
        Code = code;
    }
}
