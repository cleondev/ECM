using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ECM.Document.Api.Documents;

public sealed class CreateDocumentRequest
{
    [Required]
    public string Title { get; init; } = string.Empty;

    [Required]
    public string DocType { get; init; } = string.Empty;

    [Required]
    public string Status { get; init; } = string.Empty;

    [Required]
    public Guid OwnerId { get; init; }

    [Required]
    public Guid CreatedBy { get; init; }

    public string? Department { get; init; }

    public string? Sensitivity { get; init; }

    public Guid? DocumentTypeId { get; init; }

    [Required]
    public IFormFile File { get; init; } = null!;
}
