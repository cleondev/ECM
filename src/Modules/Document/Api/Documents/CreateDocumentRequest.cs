using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

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
    public IFormFile? File { get; init; }

    public static async ValueTask<CreateDocumentRequest?> BindAsync(HttpContext context)
    {
        if (!context.Request.HasFormContentType)
        {
            return null;
        }

        var form = await context.Request.ReadFormAsync(context.RequestAborted);

        var request = new CreateDocumentRequest
        {
            Title = GetString(form, nameof(Title)),
            DocType = GetString(form, nameof(DocType)),
            Status = GetString(form, nameof(Status)),
            OwnerId = GetGuid(form, nameof(OwnerId)),
            CreatedBy = GetGuid(form, nameof(CreatedBy)),
            Department = GetString(form, nameof(Department)),
            Sensitivity = GetString(form, nameof(Sensitivity)),
            DocumentTypeId = GetGuid(form, nameof(DocumentTypeId)),
            File = GetFile(form, nameof(File))
        };

        return request;
    }

    private static string? GetString(IFormCollection form, string propertyName)
    {
        if (form.TryGetValue(propertyName, out var value) && !StringValues.IsNullOrEmpty(value))
        {
            return value.ToString();
        }

        var camelCase = char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
        if (form.TryGetValue(camelCase, out value) && !StringValues.IsNullOrEmpty(value))
        {
            return value.ToString();
        }

        return null;
    }

    private static Guid? GetGuid(IFormCollection form, string propertyName)
    {
        var value = GetString(form, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    private static IFormFile? GetFile(IFormCollection form, string propertyName)
    {
        return form.Files.GetFile(propertyName)
            ?? form.Files.GetFile(char.ToLowerInvariant(propertyName[0]) + propertyName[1..])
            ?? form.Files.FirstOrDefault();
    }
}
