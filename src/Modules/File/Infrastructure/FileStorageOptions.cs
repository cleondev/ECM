using System.ComponentModel.DataAnnotations;

namespace ECM.File.Infrastructure;

public sealed class FileStorageOptions
{
    public FileStorageProvider Provider { get; init; } = FileStorageProvider.AwsS3;

    [Required]
    public string BucketName { get; init; } = string.Empty;

    [Required]
    public string ServiceUrl { get; init; } = string.Empty;

    [Required]
    public string AccessKeyId { get; init; } = string.Empty;

    [Required]
    public string SecretAccessKey { get; init; } = string.Empty;

    [Required]
    public string Region { get; init; } = "us-east-1";

    public bool ForcePathStyle { get; init; } = true;
}
