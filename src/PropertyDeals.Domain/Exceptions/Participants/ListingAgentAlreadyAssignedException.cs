namespace PropertyDeals.Domain.Exceptions.Participants;

public sealed class ListingAgentAlreadyAssignedException : DomainException
{
    public ListingAgentAlreadyAssignedException()
        : base("deal.listing_agent_already_assigned", "This deal already has a listing agent.")
    {
    }
}
