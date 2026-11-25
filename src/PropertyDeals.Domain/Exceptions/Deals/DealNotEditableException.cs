using PropertyDeals.Domain.Enums;

namespace PropertyDeals.Domain.Exceptions.Deals;

/// <summary>The deal is in a final status (Closed or Cancelled) and cannot be changed.</summary>
public sealed class DealNotEditableException : DomainException
{
    public DealStatusEnum Status { get; }

    public DealNotEditableException(DealStatusEnum status)
        : base("deal.not_editable", $"A deal in status {status} cannot be changed.")
    {
        Status = status;
    }
}
