using System.Text.Json;
using System.Text.Json.Serialization;
using ECM.File.Application.Shares;
using ECM.File.Domain.Shares;

namespace ECM.File.Api.Shares;

public sealed class CreateShareLinkRequest
{
    [JsonPropertyName("documentId")]
    public Guid DocumentId { get; set; }

    [JsonPropertyName("versionId")]
    public Guid? VersionId { get; set; }

    [JsonPropertyName("subjectType")]
    public string SubjectType { get; set; } = "public";

    [JsonPropertyName("subjectId")]
    public Guid? SubjectId { get; set; }

    [JsonPropertyName("permissions")]
    public string[]? Permissions { get; set; }

    [JsonPropertyName("validFrom")]
    public DateTimeOffset? ValidFrom { get; set; }

    [JsonPropertyName("validTo")]
    public DateTimeOffset? ValidTo { get; set; }

    [JsonPropertyName("maxViews")]
    public int? MaxViews { get; set; }

    [JsonPropertyName("maxDownloads")]
    public int? MaxDownloads { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("fileName")]
    public string FileName { get; set; } = string.Empty;

    [JsonPropertyName("fileExtension")]
    public string? FileExtension { get; set; }

    [JsonPropertyName("fileContentType")]
    public string FileContentType { get; set; } = string.Empty;

    [JsonPropertyName("fileSizeBytes")]
    public long FileSizeBytes { get; set; }

    [JsonPropertyName("fileCreatedAt")]
    public DateTimeOffset? FileCreatedAt { get; set; }

    [JsonPropertyName("watermark")]
    public JsonElement? Watermark { get; set; }

    [JsonPropertyName("allowedIps")]
    public string[]? AllowedIps { get; set; }

    public CreateShareLinkCommand ToCommand(Guid ownerId)
    {
        var subjectType = ParseSubjectType(SubjectType);
        var permissions = ParsePermissions(Permissions);
        var watermark = Watermark is { ValueKind: not JsonValueKind.Undefined and not JsonValueKind.Null }
            ? Watermark.Value.GetRawText()
            : null;

        return new CreateShareLinkCommand(
            ownerId,
            DocumentId,
            VersionId,
            subjectType,
            SubjectId,
            permissions,
            ValidFrom,
            ValidTo,
            MaxViews,
            MaxDownloads,
            Password,
            FileName,
            FileExtension,
            FileContentType,
            FileSizeBytes,
            FileCreatedAt,
            watermark,
            AllowedIps);
    }

    private static IReadOnlyCollection<SharePermission> ParsePermissions(IEnumerable<string>? values)
    {
        if (values is null)
        {
            return [];
        }

        var list = new List<SharePermission>();
        foreach (var value in values)
        {
            if (string.Equals(value, "download", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(SharePermission.Download);
            }
            else if (string.Equals(value, "view", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(SharePermission.View);
            }
        }

        return list;
    }

    private static ShareSubjectType ParseSubjectType(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "user" => ShareSubjectType.User,
            "group" => ShareSubjectType.Group,
            _ => ShareSubjectType.Public,
        };
    }
}

public sealed class UpdateShareLinkRequest
{
    [JsonPropertyName("validFrom")]
    public DateTimeOffset? ValidFrom { get; set; }

    [JsonPropertyName("validTo")]
    public DateTimeOffset? ValidTo { get; set; }

    [JsonPropertyName("maxViews")]
    public int? MaxViews { get; set; }

    [JsonPropertyName("maxDownloads")]
    public int? MaxDownloads { get; set; }

    [JsonPropertyName("permissions")]
    public string[]? Permissions { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("removePassword")]
    public bool RemovePassword { get; set; }

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("fileExtension")]
    public string? FileExtension { get; set; }

    [JsonPropertyName("fileContentType")]
    public string? FileContentType { get; set; }

    [JsonPropertyName("fileSizeBytes")]
    public long? FileSizeBytes { get; set; }

    [JsonPropertyName("fileCreatedAt")]
    public DateTimeOffset? FileCreatedAt { get; set; }

    [JsonPropertyName("watermark")]
    public JsonElement? Watermark { get; set; }

    [JsonPropertyName("allowedIps")]
    public string[]? AllowedIps { get; set; }

    public UpdateShareLinkCommand ToCommand(Guid shareId)
    {
        var permissions = Permissions is null ? null : ParsePermissions(Permissions);
        var watermark = Watermark is { ValueKind: not JsonValueKind.Undefined and not JsonValueKind.Null }
            ? Watermark.Value.GetRawText()
            : null;

        return new UpdateShareLinkCommand(
            shareId,
            ValidFrom,
            ValidTo,
            MaxViews,
            MaxDownloads,
            permissions,
            Password,
            RemovePassword,
            FileName,
            FileExtension,
            FileContentType,
            FileSizeBytes,
            FileCreatedAt,
            watermark,
            AllowedIps);
}

    private static IReadOnlyCollection<SharePermission> ParsePermissions(IEnumerable<string> values)
    {
        var list = new List<SharePermission>();
        foreach (var value in values)
        {
            if (string.Equals(value, "download", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(SharePermission.Download);
            }
            else if (string.Equals(value, "view", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(SharePermission.View);
            }
        }

        return list;
    }
}

public sealed record VerifySharePasswordRequest([property: JsonPropertyName("password")] string Password);

public sealed record SharePresignRequest([property: JsonPropertyName("password")] string? Password);
