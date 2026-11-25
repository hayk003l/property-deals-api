using PropertyDeals.Domain.Enums;

namespace PropertyDeals.Domain.Exceptions.Deals;

public sealed class InvalidDealStatusTransitionException : DomainException
{
    public DealStatusEnum From { get; }
    public DealStatusEnum To { get; }

    public InvalidDealStatusTransitionException(DealStatusEnum from, DealStatusEnum to)
        : base("deal.invalid_status_transition", $"A deal cannot move from {from} to {to}.")
    {
        From = from;
        To = to;
    }
}
