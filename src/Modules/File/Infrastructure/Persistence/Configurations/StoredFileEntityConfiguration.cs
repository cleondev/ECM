using ECM.File.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.File.Infrastructure.Persistence.Configurations;

public sealed class StoredFileEntityConfiguration : IEntityTypeConfiguration<StoredFileEntity>
{
    public void Configure(EntityTypeBuilder<StoredFileEntity> builder)
    {
        builder.ToTable("file_object");

        builder.HasKey(file => file.StorageKey);

        builder.Property(file => file.StorageKey)
            .HasColumnName("storage_key")
            .IsRequired();

        builder.Property(file => file.LegalHold)
            .HasColumnName("legal_hold")
            .HasDefaultValue(false);

        builder.Property(file => file.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");
    }
}
