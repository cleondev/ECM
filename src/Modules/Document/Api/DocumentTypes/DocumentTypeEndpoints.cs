using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.Document.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace ECM.Document.Api.DocumentTypes;

public static class DocumentTypeEndpoints
{
    public static void MapDocumentTypeEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/ecm/document-types");
        group.WithTags("Document Types");
        group.WithGroupName(DocumentSwagger.DocumentName);

        group
            .MapGet("/", ListDocumentTypesAsync)
            .WithName("ListDocumentTypes")
            .WithDescription("Retrieve the list of document types that can be assigned to documents.");
    }

    private static async Task<Ok<DocumentTypeResponse[]>> ListDocumentTypesAsync(
        DocumentDbContext context,
        CancellationToken cancellationToken)
    {
        var documentTypes = await context.DocumentTypes
            .AsNoTracking()
            .Where(type => type.IsActive)
            .OrderBy(type => type.TypeName)
            .ToListAsync(cancellationToken);

        var response = documentTypes
            .Select(type => new DocumentTypeResponse(
                type.Id,
                type.TypeKey,
                type.TypeName,
                type.IsActive,
                type.CreatedAtUtc))
            .ToArray();

        return TypedResults.Ok(response);
    }
}
