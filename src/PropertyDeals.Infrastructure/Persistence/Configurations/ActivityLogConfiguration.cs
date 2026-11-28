using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyDeals.Domain.Entities;

namespace PropertyDeals.Infrastructure.Persistence.Configurations;

public class ActivityLogConfiguration : IEntityTypeConfiguration<ActivityLog>
{
    public void Configure(EntityTypeBuilder<ActivityLog> builder)
    {
        builder.ToTable("activity_logs");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Details).HasMaxLength(ActivityLog.DetailsMaxLength);

        builder.Property(a => a.Action).HasConversion<string>().HasMaxLength(ColumnLengths.Enum);

        builder.HasOne<Deal>().WithMany().HasForeignKey(a => a.DealId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.DealId, a.OccurredOnUtc });
    }
}
