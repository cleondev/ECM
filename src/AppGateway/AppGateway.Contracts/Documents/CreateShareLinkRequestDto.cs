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
    string SubjectType = "public",
    Guid? SubjectId = null,
    int? ExpiresInMinutes = null)
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

    public string GetNormalizedSubjectType()
    {
        if (string.IsNullOrWhiteSpace(SubjectType))
        {
            return "public";
        }

        return SubjectType.Trim().ToLowerInvariant() switch
        {
            "user" => "user",
            "group" => "group",
            _ => "public",
        };
    }

    public Guid? GetEffectiveSubjectId()
    {
        var normalizedType = GetNormalizedSubjectType();
        if (normalizedType == "public")
        {
            return null;
        }

        if (!SubjectId.HasValue || SubjectId.Value == Guid.Empty)
        {
            return null;
        }

        return SubjectId;
    }

    public bool IsPublicShare => GetNormalizedSubjectType() == "public";
}
