using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyDeals.Domain.Entities;
using PropertyDeals.Domain.ValueObjects;

namespace PropertyDeals.Infrastructure.Persistence.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("properties");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Type).HasConversion<string>().HasMaxLength(ColumnLengths.Enum);

        builder.HasOne<User>().WithMany().HasForeignKey(p => p.ListedById).OnDelete(DeleteBehavior.Restrict);

        builder.ComplexProperty(p => p.Price, price =>
        {
            price.Property(p => p.Amount).HasPrecision(Money.Precision, Money.Scale);
            price.Property(p => p.Currency).HasMaxLength(Money.CurrencyLength).IsFixedLength();
        });

        builder.ComplexProperty(p => p.Address, address =>
        {
            address.Property(p => p.Country).HasMaxLength(Address.CountryMaxLength).IsFixedLength();
            address.Property(p => p.State).HasMaxLength(Address.StateMaxLength);
            address.Property(p => p.ZipCode).HasMaxLength(Address.ZipCodeMaxLength);
            address.Property(p => p.City).HasMaxLength(Address.CityMaxLength);
            address.Property(p => p.Street).HasMaxLength(Address.StreetMaxLength);
        });

        builder.Ignore(p => p.DomainEvents);
    }
}
