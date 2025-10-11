using ECM.Document.Domain.Signatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Document.Infrastructure.Persistence.Configurations;

public sealed class SignatureResultConfiguration : IEntityTypeConfiguration<SignatureResult>
{
    public void Configure(EntityTypeBuilder<SignatureResult> builder)
    {
        builder.ToTable("signature_result");

        builder.HasKey(result => result.RequestId);

        builder.Property(result => result.RequestId)
            .HasColumnName("request_id");

        builder.Property(result => result.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(result => result.EvidenceHash)
            .HasColumnName("evidence_hash");

        builder.Property(result => result.EvidenceUrl)
            .HasColumnName("evidence_url");

        builder.Property(result => result.ReceivedAtUtc)
            .HasColumnName("received_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.Property(result => result.RawResponse)
            .HasColumnName("raw_response")
            .ConfigureJsonDocument();

        builder.HasOne(result => result.Request)
            .WithOne(request => request.Result)
            .HasForeignKey<SignatureResult>(result => result.RequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
