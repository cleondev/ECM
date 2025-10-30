using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Documents.AccessControl;
using ECM.Document.Infrastructure.Persistence;
using ECM.Document.Infrastructure.Persistence.ReadModels;
using Microsoft.EntityFrameworkCore;

namespace ECM.Document.Infrastructure.AccessControl;

public sealed class EffectiveAclFlatWriter : IEffectiveAclFlatWriter
{
    private readonly DocumentDbContext _context;
    private readonly ISystemClock _clock;

    public EffectiveAclFlatWriter(DocumentDbContext context, ISystemClock clock)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public Task UpsertAsync(EffectiveAclFlatWriteEntry entry, CancellationToken cancellationToken = default)
    {
        return UpsertAsync(new[] { entry }, cancellationToken);
    }

    public async Task UpsertAsync(IEnumerable<EffectiveAclFlatWriteEntry> entries, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var buffer = entries as EffectiveAclFlatWriteEntry[] ?? entries.ToArray();
        if (buffer.Length == 0)
        {
            return;
        }

        var now = _clock.UtcNow;

        foreach (var entry in buffer)
        {
            var entity = await _context.EffectiveAclEntries.FindAsync(
                new object[] { entry.DocumentId, entry.UserId, entry.IdempotencyKey },
                cancellationToken);

            if (entity is null)
            {
                entity = new EffectiveAclFlatEntry
                {
                    DocumentId = entry.DocumentId,
                    UserId = entry.UserId,
                    ValidToUtc = entry.ValidToUtc,
                    Source = entry.Source,
                    IdempotencyKey = entry.IdempotencyKey,
                    UpdatedAtUtc = now,
                };

                await _context.EffectiveAclEntries.AddAsync(entity, cancellationToken);
            }
            else
            {
                entity.ValidToUtc = entry.ValidToUtc;
                entity.Source = entry.Source;
                entity.UpdatedAtUtc = now;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}
