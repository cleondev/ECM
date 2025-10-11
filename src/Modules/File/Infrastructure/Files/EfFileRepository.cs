using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.File.Application.Files;
using ECM.File.Domain.Files;
using ECM.File.Domain.Files.Events;
using ECM.File.Infrastructure.Outbox;
using ECM.File.Infrastructure.Persistence;
using ECM.File.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace ECM.File.Infrastructure.Files;

public sealed class EfFileRepository(FileDbContext context) : IFileRepository
{
    private readonly FileDbContext _context = context;

    public async Task<StoredFile> AddAsync(StoredFile file, CancellationToken cancellationToken = default)
    {
        var entity = new StoredFileEntity
        {
            StorageKey = file.StorageKey,
            LegalHold = file.LegalHold,
            CreatedAtUtc = file.CreatedAtUtc
        };

        _context.StoredFiles.Add(entity);

        var outboxMessages = FileOutboxMapper
            .ToOutboxMessages(file.DomainEvents)
            .ToArray();

        if (outboxMessages.Length > 0)
        {
            await _context.OutboxMessages.AddRangeAsync(outboxMessages, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        file.ClearDomainEvents();

        return new StoredFile(entity.StorageKey, entity.LegalHold, entity.CreatedAtUtc);
    }

    public async Task<IReadOnlyCollection<StoredFile>> GetRecentAsync(int limit, CancellationToken cancellationToken = default)
    {
        var items = await _context.StoredFiles
            .OrderByDescending(file => file.CreatedAtUtc)
            .Take(limit)
            .Select(file => new StoredFile(file.StorageKey, file.LegalHold, file.CreatedAtUtc))
            .ToArrayAsync(cancellationToken);

        return items;
    }
}
