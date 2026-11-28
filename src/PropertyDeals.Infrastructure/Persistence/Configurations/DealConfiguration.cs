using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyDeals.Domain.Entities;

namespace PropertyDeals.Infrastructure.Persistence.Configurations;

public class DealConfiguration : IEntityTypeConfiguration<Deal>
{
    public void Configure(EntityTypeBuilder<Deal> builder)
    {
        builder.ToTable("deals");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(ColumnLengths.Enum);

        builder.HasOne<Property>().WithMany().HasForeignKey(d => d.PropertyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(d => d.CreatedBy).OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(d => d.DealParticipants)
            .WithOne(p => p.Deal)
            .HasForeignKey(p => p.DealId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(d => d.DealParticipants)
            .HasField("_dealParticipants")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(d => new { d.Status, d.ClosingDate });

        builder.Ignore(d => d.DomainEvents);
    }
}
