namespace PropertyDeals.Domain.Events;

/// <summary>
/// Base record for all domain events. Id and OccurredOnUtc are set once, when the event is created.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOnUtc { get; }

    protected DomainEvent(DateTime occurredOnUtc)
    {
        if (occurredOnUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException(
                $"OccurredOnUtc must be a UTC time, but its Kind is {occurredOnUtc.Kind}.",
                nameof(occurredOnUtc));
        }

        OccurredOnUtc = occurredOnUtc;
    }
}
