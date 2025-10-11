using System.Linq;
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
        => _context.TagLabels.FirstOrDefaultAsync(label => label.Id == tagId, cancellationToken);

    public Task<TagLabel?> GetByNamespaceAndPathAsync(string namespaceSlug, string path, CancellationToken cancellationToken = default)
        => _context.TagLabels.FirstOrDefaultAsync(label => label.NamespaceSlug == namespaceSlug && label.Path == path, cancellationToken);

    public Task<bool> NamespaceExistsAsync(string namespaceSlug, CancellationToken cancellationToken = default)
        => _context.TagNamespaces.AnyAsync(ns => ns.NamespaceSlug == namespaceSlug, cancellationToken);

    public async Task<TagLabel> AddAsync(TagLabel tagLabel, CancellationToken cancellationToken = default)
    {
        await _context.TagLabels.AddAsync(tagLabel, cancellationToken);
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
