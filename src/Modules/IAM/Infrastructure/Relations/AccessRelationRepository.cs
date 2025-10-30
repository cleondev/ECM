using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.IAM.Application.Relations;
using ECM.IAM.Domain.Relations;
using ECM.IAM.Infrastructure.Outbox;
using ECM.IAM.Infrastructure.Persistence;
using ECM.BuildingBlocks.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace ECM.IAM.Infrastructure.Relations;

public sealed class AccessRelationRepository(IamDbContext context) : IAccessRelationRepository
{
    private readonly IamDbContext _context = context;

    public async Task<IReadOnlyCollection<AccessRelation>> GetBySubjectAsync(string subjectType, Guid subjectId, CancellationToken cancellationToken = default)
        => await _context.Relations
            .Where(relation => relation.SubjectType == subjectType && relation.SubjectId == subjectId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<AccessRelation>> GetByObjectAsync(string objectType, Guid objectId, CancellationToken cancellationToken = default)
        => await _context.Relations
            .Where(relation => relation.ObjectType == objectType && relation.ObjectId == objectId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

    public async Task<AccessRelation?> GetAsync(string subjectType, Guid subjectId, string objectType, Guid objectId, string relation, CancellationToken cancellationToken = default)
        => await _context.Relations.FirstOrDefaultAsync(
            candidate => candidate.SubjectType == subjectType
                         && candidate.SubjectId == subjectId
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
