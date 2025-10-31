using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Http;

using Shared.Extensions.Http;

namespace AppGateway.Api.Controllers.Documents;

public sealed class CreateDocumentsForm
{
    [StringLength(256)]
    public string? Title { get; init; }

    [StringLength(128)]
    public string? DocType { get; init; }

    [StringLength(64)]
    public string? Status { get; init; }

    public Guid? OwnerId { get; init; }

    public Guid? CreatedBy { get; init; }

    [StringLength(64)]
    public string? Sensitivity { get; init; }

    public Guid? DocumentTypeId { get; init; }

    public string? FlowDefinition { get; init; }

    public IReadOnlyList<Guid> TagIds { get; init; } = [];

    public IReadOnlyList<IFormFile> Files { get; init; } = [];

    public static async ValueTask<CreateDocumentsForm?> BindAsync(HttpContext context)
    {
        if (!context.Request.HasFormContentType)
        {
            return null;
        }

        var form = await context.Request.ReadFormAsync(context.RequestAborted);

        var request = new CreateDocumentsForm
        {
            Title = DocumentFormFieldReader.GetString(form, nameof(Title)),
            DocType = DocumentFormFieldReader.GetString(form, nameof(DocType)),
            Status = DocumentFormFieldReader.GetString(form, nameof(Status)),
            OwnerId = DocumentFormFieldReader.GetGuid(form, nameof(OwnerId)),
            CreatedBy = DocumentFormFieldReader.GetGuid(form, nameof(CreatedBy)),
            Sensitivity = DocumentFormFieldReader.GetString(form, nameof(Sensitivity)),
            DocumentTypeId = DocumentFormFieldReader.GetGuid(form, nameof(DocumentTypeId)),
            FlowDefinition = DocumentFormFieldReader.GetString(form, nameof(FlowDefinition)),
            TagIds = DocumentFormFieldReader.GetGuidList(form, "Tags"),
            Files = DocumentFormFieldReader.GetFiles(form),
        };

        return request;
    }
}
