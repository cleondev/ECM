using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

using ECM.BuildingBlocks.Application.Abstractions.Time;

using ECM.Abstractions.Files;
using ECM.Document.Api;
using ECM.Document.Application.Documents.Queries;
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
    private static readonly TimeSpan DownloadLinkLifetime = TimeSpan.FromMinutes(10);

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

        group.MapGet("/files/download/{versionId:guid}", DownloadFileAsync)
             .WithName("DownloadDocumentVersion")
             .WithDescription("Redirects to a signed URL for downloading a document version.");

        group.MapGet("/files/preview/{versionId:guid}", PreviewFileAsync)
             .WithName("PreviewDocumentVersion")
             .WithDescription("Streams the original file content for preview purposes.");

        group.MapGet("/files/thumbnails/{versionId:guid}", GetThumbnailAsync)
             .WithName("GetDocumentThumbnail")
             .WithDescription("Returns a generated thumbnail for the requested document version.");

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

    private static async Task<IResult> DownloadFileAsync(
        Guid versionId,
        IDocumentVersionReadService versionReadService,
        IFileAccessGateway fileAccess,
        CancellationToken cancellationToken)
    {
        var version = await versionReadService.GetByIdAsync(versionId, cancellationToken);
        if (version is null)
        {
            return TypedResults.NotFound();
        }

        var linkResult = await fileAccess.GetDownloadLinkAsync(version.StorageKey, DownloadLinkLifetime, cancellationToken);
        if (linkResult.IsFailure || linkResult.Value is null)
        {
            return MapFileErrors(linkResult.Errors);
        }

        return TypedResults.Redirect(linkResult.Value.Uri.ToString(), permanent: false);
    }

    private static async Task<IResult> PreviewFileAsync(
        Guid versionId,
        IDocumentVersionReadService versionReadService,
        IFileAccessGateway fileAccess,
        CancellationToken cancellationToken)
    {
        var version = await versionReadService.GetByIdAsync(versionId, cancellationToken);
        if (version is null)
        {
            return TypedResults.NotFound();
        }

        var contentResult = await fileAccess.GetContentAsync(version.StorageKey, cancellationToken);
        if (contentResult.IsFailure || contentResult.Value is null)
        {
            return MapFileErrors(contentResult.Errors);
        }

        var file = contentResult.Value;
        return TypedResults.File(
            fileContents: file.Content,
            contentType: file.ContentType,
            fileDownloadName: file.FileName,
            enableRangeProcessing: true,
            lastModified: file.LastModifiedUtc);
    }

    private static async Task<IResult> GetThumbnailAsync(
        Guid versionId,
        [FromQuery(Name = "w")] int? width,
        [FromQuery(Name = "h")] int? height,
        [FromQuery(Name = "fit")] string? fit,
        IDocumentVersionReadService versionReadService,
        IFileAccessGateway fileAccess,
        CancellationToken cancellationToken)
    {
        if (width is null or <= 0 || height is null or <= 0)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["dimensions"] = ["Parameters 'w' and 'h' must be positive integers."]
            }, statusCode: StatusCodes.Status400BadRequest);
        }

        var normalizedFit = string.IsNullOrWhiteSpace(fit)
            ? "cover"
            : fit.Trim().ToLowerInvariant();

        if (normalizedFit != "cover" && normalizedFit != "contain")
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["fit"] = ["Parameter 'fit' must be either 'cover' or 'contain'."]
            }, statusCode: StatusCodes.Status400BadRequest);
        }

        var version = await versionReadService.GetByIdAsync(versionId, cancellationToken);
        if (version is null)
        {
            return TypedResults.NotFound();
        }

        var thumbnailResult = await fileAccess.GetThumbnailAsync(
            version.StorageKey,
            width.Value,
            height.Value,
            normalizedFit,
            cancellationToken);

        if (thumbnailResult.IsFailure || thumbnailResult.Value is null)
        {
            return MapFileErrors(thumbnailResult.Errors);
        }

        var thumbnail = thumbnailResult.Value;
        return TypedResults.File(
            fileContents: thumbnail.Content,
            contentType: thumbnail.ContentType,
            fileDownloadName: thumbnail.FileName,
            enableRangeProcessing: false,
            lastModified: thumbnail.LastModifiedUtc);
    }

    private static IResult MapFileErrors(IReadOnlyCollection<string> errors)
    {
        if (errors.Count == 0)
        {
            return TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError);
        }

        if (errors.Any(error => string.Equals(error, "NotFound", StringComparison.OrdinalIgnoreCase)))
        {
            return TypedResults.NotFound();
        }

        if (errors.Any(error => string.Equals(error, "StorageKeyRequired", StringComparison.OrdinalIgnoreCase)))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["storageKey"] = ["A valid storage key is required to access the file."]
            }, statusCode: StatusCodes.Status400BadRequest);
        }

        return TypedResults.Problem(
            detail: string.Join("; ", errors),
            statusCode: StatusCodes.Status503ServiceUnavailable);
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
