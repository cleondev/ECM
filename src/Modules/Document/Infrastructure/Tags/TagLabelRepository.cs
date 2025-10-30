using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Domain.Events;
using ECM.Document.Application.Tags.Repositories;
using ECM.Document.Domain.Tags;
using ECM.Document.Infrastructure.Outbox;
using ECM.Document.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECM.Document.Infrastructure.Tags;

public sealed class TagLabelRepository(DocumentDbContext context) : ITagLabelRepository
{
    private readonly DocumentDbContext _context = context;

    public Task<TagLabel?> GetByIdAsync(Guid tagId, CancellationToken cancellationToken = default)
        => _context.TagLabels
            .Include(label => label.Parent)
            .FirstOrDefaultAsync(label => label.Id == tagId, cancellationToken);

    public Task<TagLabel[]> ListWithNamespaceAsync(CancellationToken cancellationToken = default)
        => _context.TagLabels
            .AsNoTracking()
            .Include(label => label.Namespace)
            .OrderBy(label => label.NamespaceId)
            .ThenBy(label => label.ParentId.HasValue ? 1 : 0)
            .ThenBy(label => label.SortOrder)
            .ThenBy(label => label.Name)
            .ToArrayAsync(cancellationToken);

    public Task<bool> ExistsWithNameAsync(
        Guid namespaceId,
        Guid? parentId,
        string name,
        Guid? excludeTagId,
        CancellationToken cancellationToken = default)
    {
        var query = _context.TagLabels.AsNoTracking().Where(label => label.NamespaceId == namespaceId && label.Name == name);

        query = parentId.HasValue
            ? query.Where(label => label.ParentId == parentId.Value)
            : query.Where(label => label.ParentId == null);

        if (excludeTagId.HasValue)
        {
            query = query.Where(label => label.Id != excludeTagId.Value);
        }

        return query.AnyAsync(cancellationToken);
    }

    public Task<bool> HasChildrenAsync(Guid tagId, CancellationToken cancellationToken = default)
        => _context.TagLabels.AsNoTracking().AnyAsync(label => label.ParentId == tagId, cancellationToken);

    public async Task<TagLabel> AddAsync(TagLabel tagLabel, CancellationToken cancellationToken = default)
    {
        await _context.TagLabels.AddAsync(tagLabel, cancellationToken);
        await EnqueueOutboxMessagesAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return tagLabel;
    }

    public async Task<TagLabel> UpdateAsync(TagLabel tagLabel, CancellationToken cancellationToken = default)
    {
        _context.TagLabels.Update(tagLabel);
        await EnqueueOutboxMessagesAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return tagLabel;
    }

    public async Task RemoveAsync(TagLabel tagLabel, CancellationToken cancellationToken = default)
    {
        _context.TagLabels.Remove(tagLabel);
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
            .SelectMany(entity => DocumentOutboxMapper.ToOutboxMessages(entity.DomainEvents))
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
