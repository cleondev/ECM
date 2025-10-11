using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.AccessControl.Application.Users;
using ECM.AccessControl.Domain.Users;
using ECM.AccessControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECM.AccessControl.Infrastructure.Users;

public sealed class UserRepository(AccessControlDbContext context) : IUserRepository
{
    private readonly AccessControlDbContext _context = context;

    public async Task<IReadOnlyCollection<User>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Users
            .Include(user => user.Roles)
            .ThenInclude(link => link.Role)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Users
            .Include(user => user.Roles)
            .ThenInclude(link => link.Role)
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _context.Users
            .Include(user => user.Roles)
            .ThenInclude(link => link.Role)
            .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
