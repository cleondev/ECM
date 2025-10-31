using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

namespace AppGateway.Api.Controllers.Documents;

public sealed class CreateDocumentForm
{
    [Required]
    [StringLength(256)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string DocType { get; init; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string Status { get; init; } = string.Empty;

    [Required]
    public Guid? OwnerId { get; init; }

    [Required]
    public Guid? CreatedBy { get; init; }

    [StringLength(64)]
    public string? Sensitivity { get; init; }

    public Guid? DocumentTypeId { get; init; }

    [Required]
    public IFormFile? File { get; init; }

    public static async ValueTask<CreateDocumentForm?> BindAsync(HttpContext context)
    {
        if (!context.Request.HasFormContentType)
        {
            return null;
        }

        var form = await context.Request.ReadFormAsync(context.RequestAborted);

        var request = new CreateDocumentForm
        {
            Title = DocumentFormFieldReader.GetString(form, nameof(Title)) ?? string.Empty,
            DocType = DocumentFormFieldReader.GetString(form, nameof(DocType)) ?? string.Empty,
            Status = DocumentFormFieldReader.GetString(form, nameof(Status)) ?? string.Empty,
            OwnerId = DocumentFormFieldReader.GetGuid(form, nameof(OwnerId)),
            CreatedBy = DocumentFormFieldReader.GetGuid(form, nameof(CreatedBy)),
            Sensitivity = DocumentFormFieldReader.GetString(form, nameof(Sensitivity)),
            DocumentTypeId = DocumentFormFieldReader.GetGuid(form, nameof(DocumentTypeId)),
            File = DocumentFormFieldReader.GetFile(form, nameof(File)),
        };

        return request;
    }
}
