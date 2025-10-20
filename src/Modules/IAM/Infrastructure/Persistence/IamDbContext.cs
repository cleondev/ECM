using ECM.IAM.Domain.Relations;
using ECM.IAM.Domain.Roles;
using ECM.IAM.Domain.Users;
using ECM.Outbox.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECM.IAM.Infrastructure.Persistence;

public sealed class IamDbContext(DbContextOptions<IamDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<AccessRelation> Relations => Set<AccessRelation>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("iam");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(IamDbContext).Assembly);
    }
}
