using ECM.Document.Domain.Documents;
using ECM.Document.Domain.DocumentTypes;
using ECM.Document.Domain.Files;
using ECM.Document.Domain.Signatures;
using ECM.Document.Domain.Tags;
using ECM.Document.Domain.Versions;
using ECM.Document.Infrastructure.Persistence.Models;
using ECM.Operations.Infrastructure.Persistence;
using ECM.Operations.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using DomainDocument = ECM.Document.Domain.Documents.Document;
using ECM.Document.Infrastructure.Persistence.ReadModels;

namespace ECM.Document.Infrastructure.Persistence;

public sealed class DocumentDbContext(DbContextOptions<DocumentDbContext> options) : DbContext(options)
{
    public DbSet<DomainDocument> Documents => Set<DomainDocument>();

    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();

    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();

    public DbSet<DocumentMetadata> DocumentMetadata => Set<DocumentMetadata>();

    public DbSet<FileObject> FileObjects => Set<FileObject>();

    public DbSet<TagNamespace> TagNamespaces => Set<TagNamespace>();

    public DbSet<TagLabel> TagLabels => Set<TagLabel>();

    public DbSet<DocumentTag> DocumentTags => Set<DocumentTag>();

    public DbSet<SignatureRequest> SignatureRequests => Set<SignatureRequest>();

    public DbSet<SignatureResult> SignatureResults => Set<SignatureResult>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<EffectiveAclFlatEntry> EffectiveAclEntries => Set<EffectiveAclFlatEntry>();

    public DbSet<ShareLinkEntity> ShareLinks => Set<ShareLinkEntity>();

    public DbSet<ShareAccessEventEntity> ShareAccessEvents => Set<ShareAccessEventEntity>();

    public DbSet<ShareStatisticsView> ShareStatistics => Set<ShareStatisticsView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("doc");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocumentDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(excludeFromMigrations: true));
    }
}
