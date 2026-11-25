namespace PropertyDeals.Domain.Entities.Base;

/// <summary>
/// Anything with an identity. Children inside an aggregate (e.g. Participant) inherit this directly.
/// </summary>
public abstract class Entity
{
    public int Id { get; protected set; }
}
