using PropertyDeals.Domain.Entities.Base;
using PropertyDeals.Domain.Enums;
using PropertyDeals.Domain.Events.Properties;
using PropertyDeals.Domain.Exceptions;
using PropertyDeals.Domain.ValueObjects;
using System.Diagnostics.CodeAnalysis;

namespace PropertyDeals.Domain.Entities;

public class Property : AggregateRoot
{
    public Address Address { get; private set; } = null!;
    public PropertyTypeEnum Type { get; private set; }
    public Money Price { get; private set; } = null!;
    public int Bedrooms { get; private set; }
    public int ListedById { get; private set; }

    private Property() { }

    public static Property Create(Address address,
        PropertyTypeEnum type,
        Money price,
        int bedrooms,
        int listedById,
        DateTime utcNow)
    {
        Guard.EnsureUtc(utcNow);
        EnsureValidPrice(price);

        if (!Enum.IsDefined(type))
        {
            throw new DomainValidationException($"'{type}' is not a valid property type.");
        }

        if (bedrooms < 0)
        {
            throw new DomainValidationException("Property bedroom count cannot be negative.");
        }

        if (listedById <= 0)
        {
            throw new DomainValidationException("ListedBy Id must be positive.");
        }

        if (address is null)
        {
            throw new DomainValidationException("Address cannot be null.");
        }

        var property = new Property { Address = address, Type = type, Price = price, Bedrooms = bedrooms, ListedById = listedById };
        property.RaiseDomainEvent(new PropertyCreatedEvent(property, utcNow));

        return property;
    }

    public void ChangePrice(Money newPrice, int changedBy, DateTime utcNow)
    {
        Guard.EnsureUtc(utcNow);
        EnsureValidPrice(newPrice);

        if (changedBy <= 0)
        {
            throw new DomainValidationException("ChangedBy Id must be positive.");
        }

        if (Price == newPrice)
        {
            return;
        }

        if (Price.Currency != newPrice.Currency)
        {
            throw new DomainValidationException("New and old price's currency must be the same.");
        }

        var oldPrice = Price;
        Price = newPrice;
        RaiseDomainEvent(new PropertyPriceChangedEvent(Id, oldPrice, newPrice, changedBy, utcNow));
    }

    private static void EnsureValidPrice([NotNull] Money? price)
    {
        if (price is null)
        {
            throw new DomainValidationException("Property price cannot be null.");
        }

        if (price.Amount <= 0)
        {
            throw new DomainValidationException("Property price must be positive.");
        }
    }
}
