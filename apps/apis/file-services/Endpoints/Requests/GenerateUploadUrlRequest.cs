using System.ComponentModel.DataAnnotations;

namespace FileServices.Endpoints.Requests;

public sealed class GenerateUploadUrlRequest
{
    [Required]
    [MaxLength(255)]
    public string FileName { get; init; } = string.Empty;

    [Required]
    [MaxLength(128)]
    public string ContentType { get; init; } = string.Empty;
}
