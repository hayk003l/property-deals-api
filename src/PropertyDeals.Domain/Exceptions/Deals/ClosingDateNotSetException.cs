using PropertyDeals.Domain.Enums;

namespace PropertyDeals.Domain.Exceptions.Deals;

/// <summary>The deal has no closing date yet (it has not gone under contract), so there is nothing to reschedule.</summary>
public sealed class ClosingDateNotSetException : DomainException
{
    public DealStatusEnum Status { get; }

    public ClosingDateNotSetException(DealStatusEnum status)
        : base("deal.closing_date_not_set", $"A deal in status {status} has no closing date to reschedule.")
    {
        Status = status;
    }
}
