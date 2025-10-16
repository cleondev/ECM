using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

using ECM.BuildingBlocks.Application.Abstractions.Time;

using ECM.Document.Api;
using ECM.Document.Application.Documents.Repositories;
using ECM.Document.Application.Documents.Commands;
using ECM.Document.Domain.Documents;
using ECM.Document.Infrastructure.Persistence;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace ECM.Document.Api.Documents;

public static class DocumentEndpoints
{
    public static RouteGroupBuilder MapDocumentEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/ecm");
        group.WithTags("ECM");
        group.WithGroupName(DocumentSwagger.DocumentName);

        group.MapGet("/", () => Results.Ok(new { message = "ECM API ready" }))
             .WithName("GetEcmStatus")
             .WithDescription("Return a readiness payload for the ECM edge API.");

        group.MapGet("/documents", ListDocumentsAsync)
             .WithName("ListDocuments")
             .WithDescription("Liệt kê tài liệu theo các bộ lọc hỗ trợ phân trang.");

        group.MapPost("/documents", CreateDocumentAsync)
             .WithName("CreateDocument")
             .WithDescription("Create a document using the application service (Clean Architecture).");

        group.MapGet("/documents/{id:guid}", GetDocumentAsync)
             .WithName("GetDocument")
             .WithDescription("Lấy chi tiết tài liệu kèm phiên bản gần nhất.");

        group.MapPatch("/documents/{id:guid}", UpdateDocumentAsync)
             .WithName("UpdateDocument")
             .WithDescription("Cập nhật các thuộc tính cơ bản của tài liệu.");

        group.MapDelete("/documents/{id:guid}", DeleteDocumentAsync)
             .WithName("DeleteDocument")
             .WithDescription("Xóa tài liệu (mặc định là soft delete, hard delete khi hard=true).");

        group.MapGet("/documents/{id:guid}/metadata", GetDocumentMetadataAsync)
             .WithName("GetDocumentMetadata")
             .WithDescription("Lấy metadata (key-value) của tài liệu.");

        group.MapPut("/documents/{id:guid}/metadata", UpsertDocumentMetadataAsync)
             .WithName("UpsertDocumentMetadata")
             .WithDescription("Ghi đè metadata của tài liệu.");

        group.MapGet("/documents/{id:guid}/history", GetDocumentHistoryAsync)
             .WithName("GetDocumentHistory")
             .WithDescription("Lịch sử thay đổi thuộc tính của tài liệu (placeholder).");

        group.MapPut("/documents/{id:guid}/folder", UpdateDocumentFolderAsync)
             .WithName("UpdateDocumentFolder")
             .WithDescription("Cập nhật thư mục chứa tài liệu.");

        return group;
    }

    private static async Task<Ok<DocumentListResponse>> ListDocumentsAsync(
        [AsParameters] ListDocumentsRequest request,
        DocumentDbContext context,
        CancellationToken cancellationToken)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize <= 0 ? 24 : request.PageSize;
        pageSize = pageSize > 200 ? 200 : pageSize;

        var query = context.Documents
            .AsNoTracking()
            .Include(document => document.Versions)
            .Include(document => document.Tags)
            .Include(document => document.Metadata)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.DocType))
        {
            query = query.Where(document => document.DocType == request.DocType);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(document => document.Status == request.Status);
        }

        if (!string.IsNullOrWhiteSpace(request.Sensitivity))
        {
            query = query.Where(document => document.Sensitivity == request.Sensitivity);
        }

        if (request.OwnerId.HasValue)
        {
            query = query.Where(document => document.OwnerId == request.OwnerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Department))
        {
            query = query.Where(document => document.Department == request.Department);
        }

        if (request.Tags is { Count: > 0 })
        {
            query = query.Where(document => document.Tags.Any(tag => request.Tags.Contains(tag.TagId)));
        }

        var totalItems = await query.LongCountAsync(cancellationToken);

        query = query.OrderByDescending(document => document.UpdatedAtUtc);

        var skip = (page - 1) * pageSize;
        var documents = await query
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var items = documents
            .Select(MapDocument)
            .ToArray();

        var totalPages = totalItems == 0
            ? 0
            : (int)Math.Ceiling(totalItems / (double)pageSize);

        var response = new DocumentListResponse(page, pageSize, totalItems, totalPages, items);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Created<DocumentResponse>, ValidationProblem>> CreateDocumentAsync(
        [FromForm] CreateDocumentRequest request,
        UploadDocumentCommandHandler handler,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length <= 0)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["A non-empty file is required."]
            });
        }

        var contentType = string.IsNullOrWhiteSpace(request.File.ContentType)
            ? "application/octet-stream"
            : request.File.ContentType;

        var sha256 = await ComputeSha256Async(request.File, cancellationToken);

        await using var uploadStream = request.File.OpenReadStream();
        var command = new UploadDocumentCommand(
            request.Title,
            request.DocType,
            request.Status,
            request.OwnerId,
            request.CreatedBy,
            request.Department,
            request.Sensitivity,
            request.DocumentTypeId,
            request.File.FileName,
            contentType,
            request.File.Length,
            sha256,
            uploadStream);

        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["document"] = [.. result.Errors]
            });
        }

        var version = result.Value.LatestVersion is null
            ? null
            : new DocumentVersionResponse(
                result.Value.LatestVersion.Id,
                result.Value.LatestVersion.VersionNo,
                result.Value.LatestVersion.StorageKey,
                result.Value.LatestVersion.Bytes,
                result.Value.LatestVersion.MimeType,
                result.Value.LatestVersion.Sha256,
                result.Value.LatestVersion.CreatedBy,
                result.Value.LatestVersion.CreatedAtUtc);

        var response = new DocumentResponse(
            result.Value.Id,
            result.Value.Title,
            result.Value.DocType,
            result.Value.Status,
            result.Value.Sensitivity,
            result.Value.OwnerId,
            result.Value.CreatedBy,
            result.Value.Department,
            result.Value.CreatedAtUtc,
            result.Value.UpdatedAtUtc,
            result.Value.DocumentTypeId,
            version);

        return TypedResults.Created($"/api/ecm/documents/{response.Id}", response);
    }

    private static async Task<Results<Ok<DocumentResponse>, NotFound>> GetDocumentAsync(
        Guid id,
        IDocumentRepository repository,
        CancellationToken cancellationToken)
    {
        var document = await repository.GetAsync(DocumentId.FromGuid(id), cancellationToken);

        if (document is null)
        {
            return TypedResults.NotFound();
        }

        var response = MapDocument(document);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<Ok<DocumentResponse>, ValidationProblem, NotFound>> UpdateDocumentAsync(
        Guid id,
        UpdateDocumentRequest request,
        IDocumentRepository repository,
        ISystemClock clock,
        CancellationToken cancellationToken)
    {
        var document = await repository.GetAsync(DocumentId.FromGuid(id), cancellationToken);

        if (document is null)
        {
            return TypedResults.NotFound();
        }

        var errors = new Dictionary<string, string[]>();
        var now = clock.UtcNow;

        if (request.Title is not null)
        {
            try
            {
                document.UpdateTitle(DocumentTitle.Create(request.Title), now);
            }
            catch (Exception exception)
            {
                errors["title"] = [exception.Message];
            }
        }

        if (request.Status is not null)
        {
            try
            {
                document.UpdateStatus(request.Status, now);
            }
            catch (Exception exception)
            {
                errors["status"] = [exception.Message];
            }
        }

        if (request.Sensitivity is not null)
        {
            try
            {
                document.UpdateSensitivity(request.Sensitivity, now);
            }
            catch (Exception exception)
            {
                errors["sensitivity"] = [exception.Message];
            }
        }

        if (request.Department is not null)
        {
            document.UpdateDepartment(request.Department, now);
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        await repository.SaveChangesAsync(cancellationToken);

        var response = MapDocument(document);
        return TypedResults.Ok(response);
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> DeleteDocumentAsync(
        Guid id,
        [FromQuery] bool? hard,
        IDocumentRepository repository,
        ISystemClock clock,
        CancellationToken cancellationToken)
    {
        var document = await repository.GetAsync(DocumentId.FromGuid(id), cancellationToken);

        if (document is null)
        {
            return TypedResults.NotFound();
        }

        if (hard is true)
        {
            await repository.DeleteAsync(document, cancellationToken);
            return TypedResults.NoContent();
        }

        try
        {
            document.UpdateStatus("deleted", clock.UtcNow);
        }
        catch (Exception exception)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["status"] = [exception.Message]
            });
        }

        await repository.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<JsonElement>, NotFound>> GetDocumentMetadataAsync(
        Guid id,
        IDocumentRepository repository,
        CancellationToken cancellationToken)
    {
        var document = await repository.GetAsync(DocumentId.FromGuid(id), cancellationToken);

        if (document is null)
        {
            return TypedResults.NotFound();
        }

        if (document.Metadata is null)
        {
            using var emptyDoc = JsonDocument.Parse("{}");
            var empty = emptyDoc.RootElement.Clone();
            return TypedResults.Ok(empty);
        }

        var metadata = document.Metadata.Data.RootElement.Clone();
        return TypedResults.Ok(metadata);
    }

    private static async Task<Results<NoContent, ValidationProblem, NotFound>> UpsertDocumentMetadataAsync(
        Guid id,
        UpsertDocumentMetadataRequest request,
        IDocumentRepository repository,
        ISystemClock clock,
        CancellationToken cancellationToken)
    {
        if (request.Data.ValueKind is not JsonValueKind.Object)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["data"] = ["Metadata payload must be a JSON object."]
            });
        }

        var document = await repository.GetAsync(DocumentId.FromGuid(id), cancellationToken);

        if (document is null)
        {
            return TypedResults.NotFound();
        }

        var now = clock.UtcNow;
        var metadataDocument = JsonDocument.Parse(request.Data.GetRawText());

        if (document.Metadata is null)
        {
            document.AttachMetadata(new DocumentMetadata(document.Id, metadataDocument), now);
        }
        else
        {
            document.Metadata.Update(metadataDocument);
            document.AttachMetadata(document.Metadata, now);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static Task<Ok<DocumentHistoryListResponse>> GetDocumentHistoryAsync(
        Guid id,
        [FromQuery] int page,
        [FromQuery] int pageSize,
        CancellationToken cancellationToken)
    {
        _ = id;
        _ = page;
        _ = pageSize;
        _ = cancellationToken;

        var normalizedPage = page <= 0 ? 1 : page;
        var normalizedPageSize = pageSize <= 0 ? 24 : pageSize;
        var response = new DocumentHistoryListResponse(normalizedPage, normalizedPageSize, 0, 0, Array.Empty<DocumentHistoryEntryResponse>());
        return Task.FromResult(TypedResults.Ok(response));
    }

    private static Task<IResult> UpdateDocumentFolderAsync(
        Guid id,
        UpdateDocumentFolderRequest request,
        CancellationToken cancellationToken)
    {
        _ = id;
        _ = request;
        _ = cancellationToken;

        return Task.FromResult<IResult>(TypedResults.Problem(
            detail: "Folder updates are not implemented yet.",
            statusCode: StatusCodes.Status501NotImplemented));
    }

    private static async Task<string> ComputeSha256Async(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static DocumentResponse MapDocument(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        var latestVersion = document.Versions
            .OrderByDescending(version => version.VersionNo)
            .FirstOrDefault();

        var versionResponse = latestVersion is null
            ? null
            : new DocumentVersionResponse(
                latestVersion.Id,
                latestVersion.VersionNo,
                latestVersion.StorageKey,
                latestVersion.Bytes,
                latestVersion.MimeType,
                latestVersion.Sha256,
                latestVersion.CreatedBy,
                latestVersion.CreatedAtUtc);

        return new DocumentResponse(
            document.Id.Value,
            document.Title.Value,
            document.DocType,
            document.Status,
            document.Sensitivity,
            document.OwnerId,
            document.CreatedBy,
            document.Department,
            document.CreatedAtUtc,
            document.UpdatedAtUtc,
            document.TypeId,
            versionResponse);
    }
}
