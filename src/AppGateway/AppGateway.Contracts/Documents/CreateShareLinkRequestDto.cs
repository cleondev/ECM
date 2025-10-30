using System;

namespace AppGateway.Contracts.Documents;

public sealed record CreateShareLinkRequestDto(
    Guid DocumentId,
    Guid VersionId,
    string FileName,
    string? FileExtension,
    string FileContentType,
    long FileSizeBytes,
    DateTimeOffset? FileCreatedAtUtc,
    bool IsPublic,
    int? ExpiresInMinutes)
{
    private const int DefaultLifetimeMinutes = 1440;
    private const int MinLifetimeMinutes = 1;
    private const int MaxLifetimeMinutes = 10080;

    public int GetEffectiveMinutes()
    {
        if (ExpiresInMinutes is null)
        {
            return DefaultLifetimeMinutes;
        }

        var minutes = ExpiresInMinutes.Value;
        if (minutes < MinLifetimeMinutes)
        {
            return MinLifetimeMinutes;
        }

        if (minutes > MaxLifetimeMinutes)
        {
            return MaxLifetimeMinutes;
        }

        return minutes;
    }
}
