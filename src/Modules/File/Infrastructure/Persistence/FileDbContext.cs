using ECM.File.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace ECM.File.Infrastructure.Persistence;

public sealed class FileDbContext(DbContextOptions<FileDbContext> options) : DbContext(options)
{
    public DbSet<StoredFileEntity> StoredFiles => Set<StoredFileEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("doc");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FileDbContext).Assembly);
    }
}
