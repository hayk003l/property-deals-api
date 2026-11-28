using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PropertyDeals.Domain.Entities;

namespace PropertyDeals.Infrastructure.Persistence.Configurations;

public class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.FileName).HasMaxLength(Document.FileNameMaxLength);
        builder.Property(d => d.S3Key).HasColumnName("s3_key").HasMaxLength(Document.S3KeyMaxLength);

        builder.HasOne<Deal>().WithMany().HasForeignKey(d => d.DealId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(d => d.UploadedBy).OnDelete(DeleteBehavior.Restrict);

        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(ColumnLengths.Enum);

        builder.HasIndex(d => d.S3Key).IsUnique();
        builder.HasIndex(d => d.DealId);

        builder.Ignore(d => d.DomainEvents);
    }
}
