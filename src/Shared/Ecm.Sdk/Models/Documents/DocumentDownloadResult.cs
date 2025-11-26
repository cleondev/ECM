namespace Ecm.Sdk;

/// <summary>
/// Represents the binary content returned when downloading a document version.
/// </summary>
/// <param name="Content">Stream containing the downloaded content.</param>
/// <param name="ContentType">MIME type reported by the server.</param>
/// <param name="FileName">Suggested file name when available.</param>
/// <param name="LastModified">Server provided last-modified timestamp.</param>
public sealed record DocumentDownloadResult(
    Stream Content,
    string ContentType,
    string? FileName,
    DateTimeOffset? LastModified);
