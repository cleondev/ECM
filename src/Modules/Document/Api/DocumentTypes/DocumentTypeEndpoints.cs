using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.Document.Domain.DocumentTypes;
using ECM.Document.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

        group
            .MapPost("/", CreateDocumentTypeAsync)
            .WithName("CreateDocumentType")
            .WithDescription("Create a new document type.");

        group
            .MapPut("/{id:guid}", UpdateDocumentTypeAsync)
            .WithName("UpdateDocumentType")
            .WithDescription("Update an existing document type.");

        group
            .MapDelete("/{id:guid}", DeleteDocumentTypeAsync)
            .WithName("DeleteDocumentType")
            .WithDescription("Delete a document type.");
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
            .Select(MapToResponse)
            .ToArray();

        return TypedResults.Ok(response);
    }

    private static async Task<Results<Created<DocumentTypeResponse>, ValidationProblem>> CreateDocumentTypeAsync(
        DocumentTypeRequest request,
        DocumentDbContext context,
        CancellationToken cancellationToken)
    {
        var typeKey = request.TypeKey?.Trim();
        var typeName = request.TypeName?.Trim();

        if (string.IsNullOrWhiteSpace(typeKey) || string.IsNullOrWhiteSpace(typeName))
        {
            return ValidationProblem("documentType", "Type key and name are required.");
        }

        var typeKeyExists = await context.DocumentTypes
            .AnyAsync(type => type.TypeKey == typeKey, cancellationToken);

        if (typeKeyExists)
        {
            return ValidationProblem("typeKey", "A document type with the same key already exists.");
        }

        var documentType = new DocumentType(
            Guid.NewGuid(),
            typeKey,
            typeName,
            request.IsActive ?? true,
            DateTimeOffset.UtcNow,
            request.Description);

        context.DocumentTypes.Add(documentType);
        await context.SaveChangesAsync(cancellationToken);

        var response = MapToResponse(documentType);

        return TypedResults.Created($"/api/ecm/document-types/{documentType.Id}", response);
    }

    private static async Task<Results<Ok<DocumentTypeResponse>, ValidationProblem, NotFound>> UpdateDocumentTypeAsync(
        Guid id,
        DocumentTypeRequest request,
        DocumentDbContext context,
        CancellationToken cancellationToken)
    {
        var typeKey = request.TypeKey?.Trim();
        var typeName = request.TypeName?.Trim();

        if (string.IsNullOrWhiteSpace(typeKey) || string.IsNullOrWhiteSpace(typeName))
        {
            return ValidationProblem("documentType", "Type key and name are required.");
        }

        var documentType = await context.DocumentTypes.FindAsync([id], cancellationToken);
        if (documentType is null)
        {
            return TypedResults.NotFound();
        }

        var typeKeyInUse = await context.DocumentTypes
            .AnyAsync(type => type.TypeKey == typeKey && type.Id != id, cancellationToken);

        if (typeKeyInUse)
        {
            return ValidationProblem("typeKey", "A document type with the same key already exists.");
        }

        documentType.Update(typeKey, typeName, request.Description, request.IsActive ?? documentType.IsActive);

        await context.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(MapToResponse(documentType));
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> DeleteDocumentTypeAsync(
        Guid id,
        DocumentDbContext context,
        CancellationToken cancellationToken)
    {
        var documentType = await context.DocumentTypes.FindAsync([id], cancellationToken);
        if (documentType is null)
        {
            return TypedResults.NotFound();
        }

        var isInUse = await context.Documents
            .AnyAsync(document => document.TypeId == id, cancellationToken);

        if (isInUse)
        {
            return ValidationProblem(
                "documentType",
                "Cannot delete a document type that is assigned to existing documents."
            );
        }

        context.DocumentTypes.Remove(documentType);
        await context.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static DocumentTypeResponse MapToResponse(DocumentType type)
        => new(
            type.Id,
            type.TypeKey,
            type.TypeName,
            type.Description,
            type.IsActive,
            type.CreatedAtUtc);

    private static ValidationProblem ValidationProblem(string key, string message)
        => TypedResults.ValidationProblem(new Dictionary<string, string[]> { [key] = new[] { message } });
}
