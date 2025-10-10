using ECM.Modules.Document.Domain.Documents;
using ECM.Modules.Document.Domain.DocumentTypes;
using ECM.Modules.Document.Domain.Files;
using ECM.Modules.Document.Domain.Signatures;
using ECM.Modules.Document.Domain.Tags;
using ECM.Modules.Document.Domain.Versions;
using Microsoft.EntityFrameworkCore;

namespace ECM.Modules.Document.Infrastructure.Persistence;

public sealed class DocumentDbContext : DbContext
{
    public DocumentDbContext(DbContextOptions<DocumentDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();

    public DbSet<DocumentVersion> DocumentVersions => Set<DocumentVersion>();

    public DbSet<DocumentMetadata> DocumentMetadata => Set<DocumentMetadata>();

    public DbSet<FileObject> FileObjects => Set<FileObject>();

    public DbSet<TagNamespace> TagNamespaces => Set<TagNamespace>();

    public DbSet<TagLabel> TagLabels => Set<TagLabel>();

    public DbSet<DocumentTag> DocumentTags => Set<DocumentTag>();

    public DbSet<SignatureRequest> SignatureRequests => Set<SignatureRequest>();

    public DbSet<SignatureResult> SignatureResults => Set<SignatureResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("doc");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DocumentDbContext).Assembly);
    }
}
