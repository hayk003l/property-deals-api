namespace PropertyDeals.Domain.Exceptions.Participants;

public sealed class CannotRemoveListingAgentException : DomainException
{
    public CannotRemoveListingAgentException()
        : base("deal.cannot_remove_listing_agent", "The listing agent cannot be removed from a deal.")
    {
    }
}
