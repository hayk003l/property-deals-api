using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyDeals.Infrastructure.Outbox;

namespace PropertyDeals.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.Error).HasMaxLength(OutboxMessage.ErrorMaxLength);
        builder.Property(o => o.Type).HasMaxLength(OutboxMessage.TypeMaxLength);

        builder.Property(o => o.Payload).HasColumnType("jsonb");

        builder.HasIndex(o => o.OccurredOnUtc).HasFilter("processed_on_utc IS NULL");
    }
}
