using ECM.Operations.Infrastructure.Persistence;
using ECM.File.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace ECM.File.Infrastructure.Persistence;

public sealed class FileDbContext(DbContextOptions<FileDbContext> options) : DbContext(options)
{
    public DbSet<StoredFileEntity> StoredFiles => Set<StoredFileEntity>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public DbSet<ShareLinkEntity> ShareLinks => Set<ShareLinkEntity>();

    public DbSet<ShareAccessEventEntity> ShareAccessEvents => Set<ShareAccessEventEntity>();

    public DbSet<ShareStatisticsView> ShareStatistics => Set<ShareStatisticsView>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("file");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FileDbContext).Assembly);
    }
}
