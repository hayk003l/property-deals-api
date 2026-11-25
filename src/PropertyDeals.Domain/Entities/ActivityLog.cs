using PropertyDeals.Domain.Enums;
using PropertyDeals.Domain.Exceptions;

namespace PropertyDeals.Domain.Entities;

/// <summary>
/// Append-only history record. Not an aggregate: it never changes and raises no events,
/// so it does not inherit Entity or AggregateRoot. long Id because this table grows fastest.
/// </summary>
public class ActivityLog
{
    public const int DetailsMaxLength = 1000;

    public long Id { get; private set; }
    public int DealId { get; private set; }
    public ActivityActionEnum Action { get; private set; }
    public int UserId { get; private set; }
    public string Details { get; private set; } = null!;
    public DateTime OccurredOnUtc { get; private set; }

    private ActivityLog() { }

    public static ActivityLog Create(int dealId, ActivityActionEnum action, int userId, string details, DateTime utcNow)
    {
        Guard.EnsureUtc(utcNow);

        if (dealId <= 0 || userId <= 0)
        {
            throw new DomainValidationException("DealId and userId must be positive.");
        }

        if (!Enum.IsDefined(action))
        {
            throw new DomainValidationException($"'{action}' is not a valid activity action.");
        }

        if (string.IsNullOrWhiteSpace(details))
        {
            throw new DomainValidationException("Details cannot be empty or whitespace.");
        }

        details = details.Trim();

        if (details.Length > DetailsMaxLength)
        {
            throw new DomainValidationException($"Details cannot be longer than {DetailsMaxLength} characters.");
        }

        return new ActivityLog { DealId = dealId, Action = action, UserId = userId, Details = details, OccurredOnUtc = utcNow };
    }
}
