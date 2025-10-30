using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.IAM.Application.Users;
using ECM.IAM.Domain.Users;
using ECM.IAM.Infrastructure.Outbox;
using ECM.IAM.Infrastructure.Persistence;
using ECM.BuildingBlocks.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace ECM.IAM.Infrastructure.Users;

public sealed class UserRepository(IamDbContext context) : IUserRepository
{
    private readonly IamDbContext _context = context;

    public async Task<IReadOnlyCollection<User>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Users
            .Include(user => user.Roles)
            .ThenInclude(link => link.Role)
            .Include(user => user.Groups)
            .ThenInclude(member => member.Group)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Users
            .Include(user => user.Roles)
            .ThenInclude(link => link.Role)
            .Include(user => user.Groups)
            .ThenInclude(member => member.Group)
            .FirstOrDefaultAsync(user => user.Id == id, cancellationToken);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => await _context.Users
            .Include(user => user.Roles)
            .ThenInclude(link => link.Role)
            .Include(user => user.Groups)
            .ThenInclude(member => member.Group)
            .FirstOrDefaultAsync(user => user.Email == email, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
        await EnqueueOutboxMessagesAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        await EnqueueOutboxMessagesAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task EnqueueOutboxMessagesAsync(CancellationToken cancellationToken)
    {
        var entitiesWithEvents = _context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Select(entry => entry.Entity)
            .Where(entity => entity.DomainEvents.Count > 0)
            .ToArray();

        if (entitiesWithEvents.Length == 0)
        {
            return;
        }

        var outboxMessages = entitiesWithEvents
            .SelectMany(entity => IamOutboxMapper.ToOutboxMessages(entity.DomainEvents))
            .ToArray();

        if (outboxMessages.Length > 0)
        {
            await _context.OutboxMessages.AddRangeAsync(outboxMessages, cancellationToken);
        }

        foreach (var entity in entitiesWithEvents)
        {
            entity.ClearDomainEvents();
        }
    }
}
