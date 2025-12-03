using ECM.File.Infrastructure.Persistence.Models;
using ECM.Operations.Infrastructure.Persistence;
using ECM.Operations.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace ECM.File.Infrastructure.Persistence;

public sealed class FileDbContext(DbContextOptions<FileDbContext> options) : DbContext(options)
{
    public DbSet<StoredFileEntity> StoredFiles => Set<StoredFileEntity>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("file");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FileDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration(excludeFromMigrations: true));
    }
}
