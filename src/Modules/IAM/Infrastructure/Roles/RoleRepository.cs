using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.IAM.Application.Roles;
using ECM.IAM.Domain.Roles;
using ECM.IAM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECM.IAM.Infrastructure.Roles;

public sealed class RoleRepository(IamDbContext context) : IRoleRepository
{
    private readonly IamDbContext _context = context;

    public async Task<IReadOnlyCollection<Role>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Roles
            .Include(role => role.UserRoles)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Roles
            .Include(role => role.UserRoles)
            .ThenInclude(link => link.User)
            .FirstOrDefaultAsync(role => role.Id == id, cancellationToken);

    public async Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        => await _context.Roles
            .Include(role => role.UserRoles)
            .FirstOrDefaultAsync(role => role.Name == name, cancellationToken);

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        await _context.Roles.AddAsync(role, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        _context.Roles.Update(role);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Role role, CancellationToken cancellationToken = default)
    {
        _context.Roles.Remove(role);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
