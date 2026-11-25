using PropertyDeals.Domain.Events;

namespace PropertyDeals.Domain.Entities.Base;

/// <summary>
/// The single entry point of an aggregate: it is loaded and saved as a unit,
/// and it is the only place that raises domain events.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}
