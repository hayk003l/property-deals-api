using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyDeals.Domain.Entities;

namespace PropertyDeals.Infrastructure.Persistence.Configurations;

public class ParticipantConfiguration : IEntityTypeConfiguration<Participant>
{
    public void Configure(EntityTypeBuilder<Participant> builder)
    {
        builder.ToTable("participants");

        builder.Property(p => p.Role).HasConversion<string>().HasMaxLength(ColumnLengths.Enum);

        builder.HasOne<User>().WithMany().HasForeignKey(p => p.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.UserId, p.DealId }).IsUnique();
    }
}
