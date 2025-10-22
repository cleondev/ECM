using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ECM.Document.Api.Documents;

public sealed class CreateDocumentRequest
{
    public string? Title { get; init; }

    public string? DocType { get; init; }

    public string? Status { get; init; }

    public Guid? OwnerId { get; init; }

    public Guid? CreatedBy { get; init; }

    public string? Department { get; init; }

    public string? Sensitivity { get; init; }

    public Guid? DocumentTypeId { get; init; }

    [Required]
    public IFormFile File { get; init; } = null!;
}
