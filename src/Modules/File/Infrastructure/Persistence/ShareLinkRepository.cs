using System.Net;
using System.Linq;
using ECM.File.Application.Shares;
using ECM.File.Domain.Shares;
using ECM.File.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace ECM.File.Infrastructure.Persistence;

public sealed class ShareLinkRepository(FileDbContext context) : IShareLinkRepository
{
    private readonly FileDbContext _context = context;

    public async Task AddAsync(ShareLink shareLink, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shareLink);

        var entity = MapToEntity(shareLink);
        await _context.ShareLinks.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ShareLink?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _context.ShareLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(link => link.Id == id, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<ShareLink?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalized = code.Trim();

        var entity = await _context.ShareLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(link => link.Code == normalized, cancellationToken);

        return entity is null ? null : MapToDomain(entity);
    }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        var normalized = code.Trim();
        return await _context.ShareLinks.AnyAsync(link => link.Code == normalized, cancellationToken);
    }

    public async Task UpdateAsync(ShareLink shareLink, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(shareLink);

        var entity = await _context.ShareLinks.FirstOrDefaultAsync(link => link.Id == shareLink.Id, cancellationToken);
        if (entity is null)
        {
            return;
        }

        UpdateEntity(entity, shareLink);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ShareStatistics?> GetStatisticsAsync(Guid shareId, CancellationToken cancellationToken = default)
    {
        var view = await _context.ShareStatistics
            .AsNoTracking()
            .FirstOrDefaultAsync(stats => stats.ShareId == shareId, cancellationToken);

        return view is null
            ? null
            : new ShareStatistics(view.ShareId, view.Views, view.Downloads, view.Failures, view.LastAccess);
    }

    public async Task<long> CountSuccessfulViewsAsync(Guid shareId, CancellationToken cancellationToken = default)
    {
        return await _context.ShareAccessEvents
            .AsNoTracking()
            .LongCountAsync(evt => evt.ShareId == shareId && evt.Action == "view" && evt.Ok, cancellationToken);
    }

    public async Task<long> CountSuccessfulDownloadsAsync(Guid shareId, CancellationToken cancellationToken = default)
    {
        return await _context.ShareAccessEvents
            .AsNoTracking()
            .LongCountAsync(evt => evt.ShareId == shareId && evt.Action == "download" && evt.Ok, cancellationToken);
    }

    public async Task AddAccessEventAsync(ShareAccessEvent accessEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(accessEvent);

        var entity = new ShareAccessEventEntity
        {
            ShareId = accessEvent.ShareId,
            OccurredAt = accessEvent.OccurredAt,
            Action = accessEvent.Action,
            RemoteIp = accessEvent.RemoteIp?.ToString(),
            UserAgent = accessEvent.UserAgent,
            Ok = accessEvent.Succeeded,
        };

        await _context.ShareAccessEvents.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static ShareLink MapToDomain(ShareLinkEntity entity)
    {
        var subjectType = entity.SubjectType switch
        {
            "user" => ShareSubjectType.User,
            "group" => ShareSubjectType.Group,
            _ => ShareSubjectType.Public,
        };

        var permissions = SharePermission.None;
        foreach (var value in entity.Permissions ?? Array.Empty<string>())
        {
            if (string.Equals(value, "view", StringComparison.OrdinalIgnoreCase))
            {
                permissions |= SharePermission.View;
            }
            else if (string.Equals(value, "download", StringComparison.OrdinalIgnoreCase))
            {
                permissions |= SharePermission.Download;
            }
        }

        var allowedIps = ParseAllowedIps(entity.AllowedIps);

        return new ShareLink(
            entity.Id,
            entity.Code,
            entity.OwnerUserId,
            entity.DocumentId,
            entity.VersionId,
            subjectType,
            entity.SubjectId,
            permissions,
            entity.PasswordHash,
            entity.ValidFrom,
            entity.ValidTo,
            entity.MaxViews,
            entity.MaxDownloads,
            entity.FileName,
            entity.FileExtension,
            entity.FileContentType,
            entity.FileSizeBytes,
            entity.FileCreatedAt,
            entity.WatermarkJson,
            allowedIps,
            entity.CreatedAt,
            entity.RevokedAt);
    }

    private static ShareLinkEntity MapToEntity(ShareLink share)
    {
        return new ShareLinkEntity
        {
            Id = share.Id,
            Code = share.Code,
            OwnerUserId = share.OwnerUserId,
            DocumentId = share.DocumentId,
            VersionId = share.VersionId,
            SubjectType = share.SubjectType switch
            {
                ShareSubjectType.User => "user",
                ShareSubjectType.Group => "group",
                _ => "public",
            },
            SubjectId = share.SubjectId,
            Permissions = EnumeratePermissions(share.Permissions),
            PasswordHash = share.PasswordHash,
            ValidFrom = share.ValidFrom,
            ValidTo = share.ValidTo,
            MaxViews = share.MaxViews,
            MaxDownloads = share.MaxDownloads,
            FileName = share.FileName,
            FileExtension = share.FileExtension,
            FileContentType = share.FileContentType,
            FileSizeBytes = share.FileSizeBytes,
            FileCreatedAt = share.FileCreatedAt,
            WatermarkJson = share.WatermarkJson,
            AllowedIps = share.AllowedIps.Select(ip => ip.ToString()).ToArray(),
            CreatedAt = share.CreatedAt,
            RevokedAt = share.RevokedAt,
        };
    }

    private static void UpdateEntity(ShareLinkEntity entity, ShareLink share)
    {
        entity.Code = share.Code;
        entity.OwnerUserId = share.OwnerUserId;
        entity.DocumentId = share.DocumentId;
        entity.VersionId = share.VersionId;
        entity.SubjectType = share.SubjectType switch
        {
            ShareSubjectType.User => "user",
            ShareSubjectType.Group => "group",
            _ => "public",
        };
        entity.SubjectId = share.SubjectId;
        entity.Permissions = EnumeratePermissions(share.Permissions);
        entity.PasswordHash = share.PasswordHash;
        entity.ValidFrom = share.ValidFrom;
        entity.ValidTo = share.ValidTo;
        entity.MaxViews = share.MaxViews;
        entity.MaxDownloads = share.MaxDownloads;
        entity.FileName = share.FileName;
        entity.FileExtension = share.FileExtension;
        entity.FileContentType = share.FileContentType;
        entity.FileSizeBytes = share.FileSizeBytes;
        entity.FileCreatedAt = share.FileCreatedAt;
        entity.WatermarkJson = share.WatermarkJson;
        entity.AllowedIps = share.AllowedIps.Select(ip => ip.ToString()).ToArray();
        entity.CreatedAt = share.CreatedAt;
        entity.RevokedAt = share.RevokedAt;
    }

    private static string[] EnumeratePermissions(SharePermission permissions)
    {
        var values = new List<string>(2);
        if ((permissions & SharePermission.View) == SharePermission.View)
        {
            values.Add("view");
        }

        if ((permissions & SharePermission.Download) == SharePermission.Download)
        {
            values.Add("download");
        }

        return values.ToArray();
    }

    private static IReadOnlyCollection<IPAddress> ParseAllowedIps(string[]? values)
    {
        if (values is null || values.Length == 0)
        {
            return Array.Empty<IPAddress>();
        }

        var list = new List<IPAddress>(values.Length);
        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            var trimmed = value.Trim();
            var slashIndex = trimmed.IndexOf('/', StringComparison.Ordinal);
            if (slashIndex > 0)
            {
                trimmed = trimmed[..slashIndex];
            }

            if (IPAddress.TryParse(trimmed, out var ip))
            {
                list.Add(ip);
            }
        }

        return list;
    }
}
