using System.ComponentModel.DataAnnotations;

namespace ECM.File.Api.Files;

public sealed class RegisterFileRequest
{
    [Required]
    public string FileName { get; init; } = string.Empty;

    [Required]
    public string ContentType { get; init; } = "application/octet-stream";

    [Range(1, long.MaxValue)]
    public long Size { get; init; }

    [Required]
    public string StorageKey { get; init; } = string.Empty;
}
