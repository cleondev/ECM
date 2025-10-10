using ECM.Modules.AccessControl.Domain.Relations;
using ECM.Modules.AccessControl.Domain.Roles;
using ECM.Modules.AccessControl.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace ECM.Modules.AccessControl.Infrastructure.Persistence;

public sealed class AccessControlDbContext : DbContext
{
    public AccessControlDbContext(DbContextOptions<AccessControlDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();

    public DbSet<AccessRelation> Relations => Set<AccessRelation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("iam");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AccessControlDbContext).Assembly);
    }
}
