using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace ECM.Document.Api.Documents.Requests;

public sealed class UploadDocumentVersionRequest
{
    public Guid? CreatedBy { get; init; }

    [Required]
    public IFormFile? File { get; init; }

    public static async ValueTask<UploadDocumentVersionRequest?> BindAsync(HttpContext context)
    {
        if (!context.Request.HasFormContentType)
        {
            return null;
        }

        var form = await context.Request.ReadFormAsync(context.RequestAborted);

        return new UploadDocumentVersionRequest
        {
            CreatedBy = GetGuid(form, nameof(CreatedBy)),
            File = GetFile(form, nameof(File)),
        };
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

    private static IFormFile? GetFile(IFormCollection form, string propertyName)
    {
        return form.Files.GetFile(propertyName)
            ?? form.Files.GetFile(char.ToLowerInvariant(propertyName[0]) + propertyName[1..])
            ?? form.Files.FirstOrDefault();
    }
}
