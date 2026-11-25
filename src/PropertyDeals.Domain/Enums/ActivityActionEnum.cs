namespace PropertyDeals.Domain.Enums;

/// <summary>
/// What happened on a deal. One value per deal-related domain event.
/// Values are stored in the database, so never renumber or reuse them.
/// </summary>
public enum ActivityActionEnum
{
    DealCreated = 1,
    DealStatusChanged = 2,
    ClosingDateRescheduled = 3,
    ParticipantAdded = 4,
    ParticipantRemoved = 5,
    DocumentUploaded = 6,
    DocumentDeleted = 7
}
