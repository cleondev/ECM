using System.ComponentModel.DataAnnotations;

namespace FileServices.Configuration;

public sealed class MinioOptions
{
    public const string SectionName = "Minio";

    [Required]
    public string Endpoint { get; init; } = string.Empty;

    [Required]
    public string AccessKey { get; init; } = string.Empty;

    [Required]
    public string SecretKey { get; init; } = string.Empty;

    [Required]
    public string Bucket { get; init; } = string.Empty;

    public string Region { get; init; } = "us-east-1";

    /// <summary>
    /// Gets or sets the number of minutes that presigned URLs remain valid.
    /// Defaults to 15 minutes when not specified.
    /// </summary>
    [Range(1, 1440)]
    public int UploadUrlExpiryMinutes { get; init; } = 15;
}
