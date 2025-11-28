using ECM.BuildingBlocks.Domain.Events;
using ECM.Document.Application.Documents.Repositories;
using ECM.Document.Domain.Documents;
using ECM.Document.Infrastructure.Outbox;
using ECM.Document.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using DomainDocument = ECM.Document.Domain.Documents.Document;

namespace ECM.Document.Infrastructure.Documents;

public sealed class DocumentRepository(DocumentDbContext context) : IDocumentRepository
{
    private readonly DocumentDbContext _context = context;

    public async Task<DomainDocument> AddAsync(DomainDocument document, CancellationToken cancellationToken = default)
    {
        await _context.Documents.AddAsync(document, cancellationToken);
        await EnqueueOutboxMessagesAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return document;
    }

    public Task<DomainDocument?> GetAsync(DocumentId documentId, CancellationToken cancellationToken = default)
    {
        return _context.Documents
            .Include(document => document.Versions)
            .Include(document => document.Metadata)
            .Include(document => document.Tags)
                .ThenInclude(documentTag => documentTag.Tag)
                    .ThenInclude(tag => tag.Namespace)
            .FirstOrDefaultAsync(document => document.Id == documentId, cancellationToken);
    }

    public async Task DeleteAsync(DomainDocument document, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(document);

        _context.Documents.Remove(document);
        await EnqueueOutboxMessagesAsync(cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
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
