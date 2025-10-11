using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.AccessControl.Application.Relations;
using ECM.AccessControl.Domain.Relations;
using System.Linq;
using ECM.AccessControl.Infrastructure.Outbox;
using ECM.AccessControl.Infrastructure.Persistence;
using ECM.BuildingBlocks.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace ECM.AccessControl.Infrastructure.Relations;

public sealed class AccessRelationRepository(AccessControlDbContext context) : IAccessRelationRepository
{
    private readonly AccessControlDbContext _context = context;

    public async Task<IReadOnlyCollection<AccessRelation>> GetBySubjectAsync(Guid subjectId, CancellationToken cancellationToken = default)
        => await _context.Relations
            .Where(relation => relation.SubjectId == subjectId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<AccessRelation>> GetByObjectAsync(string objectType, Guid objectId, CancellationToken cancellationToken = default)
        => await _context.Relations
            .Where(relation => relation.ObjectType == objectType && relation.ObjectId == objectId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<AccessRelation?> GetAsync(Guid subjectId, string objectType, Guid objectId, string relation, CancellationToken cancellationToken = default)
        => await _context.Relations.FirstOrDefaultAsync(
            candidate => candidate.SubjectId == subjectId
                         && candidate.ObjectType == objectType
                         && candidate.ObjectId == objectId
                         && candidate.Relation == relation,
            cancellationToken);

    public async Task AddAsync(AccessRelation relation, CancellationToken cancellationToken = default)
    {
        await _context.Relations.AddAsync(relation, cancellationToken);
        await EnqueueOutboxMessagesAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(AccessRelation relation, CancellationToken cancellationToken = default)
    {
        _context.Relations.Remove(relation);
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
            .SelectMany(entity => AccessControlOutboxMapper.ToOutboxMessages(entity.DomainEvents))
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
