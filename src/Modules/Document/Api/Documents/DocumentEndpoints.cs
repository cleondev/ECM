using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;

using ECM.BuildingBlocks.Application.Abstractions.Time;

using ECM.Abstractions.Files;
using ECM.Document.Api;
using ECM.Document.Application.Documents.Queries;
using ECM.Document.Application.Documents.Commands;
using ECM.Document.Domain.Documents;
using ECM.Document.Infrastructure.Persistence;

using DomainDocument = ECM.Document.Domain.Documents.Document;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ECM.Document.Api.Documents;

public static class DocumentEndpoints
{
    private static readonly TimeSpan DownloadLinkLifetime = TimeSpan.FromMinutes(10);

    private const string ShareDurationValidationKey = "expiresInMinutes";

    public static RouteGroupBuilder MapDocumentEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/ecm");
        group.WithTags("ECM");
        group.WithGroupName(DocumentSwagger.DocumentName);
        group.DisableAntiforgery();

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

        group.MapPost("/files/share/{versionId:guid}", ShareFileAsync)
             .WithName("ShareDocumentVersion")
             .WithDescription("Creates a temporary share link for the requested document version.");

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
                .ThenInclude(documentTag => documentTag.Tag)
            .Include(document => document.Metadata)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var term = request.Query.Trim();
            if (term.Length > 0)
            {
                var likeExpression = $"%{term.Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal)}%";
                query = query.Where(document => EF.Functions.ILike(document.Title.Value, likeExpression));
            }
        }

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

        if (request.Tags is { Length: > 0 })
        {
            query = query.Where(document => document.Tags.Any(tag => request.Tags.Contains(tag.TagId)));
        }

        var totalItems = await query.LongCountAsync(cancellationToken);

        var orderedQuery = ApplySorting(query, request.Sort);

        var skip = (page - 1) * pageSize;
        var documents = await orderedQuery
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
        ClaimsPrincipal principal,
        [FromForm] CreateDocumentRequest request,
        UploadDocumentCommandHandler handler,
        IOptions<DocumentUploadDefaultsOptions> defaultsOptions,
        CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length <= 0)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["A non-empty file is required."]
            });
        }

        var defaults = defaultsOptions.Value ?? new DocumentUploadDefaultsOptions();
        var claimedUserId = principal.GetUserObjectId();

        var createdBy = NormalizeGuid(request.CreatedBy) ?? claimedUserId ?? defaults.CreatedBy;
        var ownerId = NormalizeGuid(request.OwnerId) ?? createdBy ?? defaults.OwnerId;

        if (createdBy is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["createdBy"] = ["The creator could not be determined from the request or user context."]
            });
        }

        if (ownerId is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["ownerId"] = ["The owner could not be determined from the request or user context."]
            });
        }

        var title = NormalizeTitle(request.Title, request.File.FileName);
        var docType = string.IsNullOrWhiteSpace(request.DocType)
            ? (string.IsNullOrWhiteSpace(defaults.DocType) ? "general" : defaults.DocType.Trim())
            : request.DocType.Trim();
        var status = string.IsNullOrWhiteSpace(request.Status)
            ? (string.IsNullOrWhiteSpace(defaults.Status) ? "draft" : defaults.Status.Trim())
            : request.Status.Trim();
        var department = string.IsNullOrWhiteSpace(request.Department)
            ? defaults.Department?.Trim()
            : request.Department?.Trim();
        var sensitivity = string.IsNullOrWhiteSpace(request.Sensitivity)
            ? (string.IsNullOrWhiteSpace(defaults.Sensitivity) ? "Internal" : defaults.Sensitivity.Trim())
            : request.Sensitivity.Trim();
        var documentTypeId = request.DocumentTypeId ?? defaults.DocumentTypeId;

        var contentType = string.IsNullOrWhiteSpace(request.File.ContentType)
            ? "application/octet-stream"
            : request.File.ContentType;

        var sha256 = await ComputeSha256Async(request.File, cancellationToken);

        await using var uploadStream = request.File.OpenReadStream();
        var command = new UploadDocumentCommand(
            title,
            docType,
            status,
            ownerId.Value,
            createdBy.Value,
            department,
            sensitivity,
            documentTypeId,
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
            version,
            []);

        return TypedResults.Created($"/api/ecm/documents/{response.Id}", response);
    }

    private static Guid? NormalizeGuid(Guid? value)
    {
        if (value is null || value == Guid.Empty)
        {
            return null;
        }

        return value;
    }

    private static string NormalizeTitle(string? title, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            return title.Trim();
        }

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            if (!string.IsNullOrWhiteSpace(nameWithoutExtension))
            {
                return nameWithoutExtension.Trim();
            }
        }

        return "Untitled document";
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

    private static async Task<IResult> ShareFileAsync(
        Guid versionId,
        ShareDocumentVersionRequest request,
        IDocumentVersionReadService versionReadService,
        IFileAccessGateway fileAccess,
        CancellationToken cancellationToken)
    {
        var version = await versionReadService.GetByIdAsync(versionId, cancellationToken);
        if (version is null)
        {
            return TypedResults.NotFound();
        }

        var shareRequest = request ?? new ShareDocumentVersionRequest();
        var normalizedMinutes = shareRequest.GetEffectiveMinutes();
        if (normalizedMinutes < ShareDocumentVersionRequest.Minimum
            || normalizedMinutes > ShareDocumentVersionRequest.Maximum)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [ShareDurationValidationKey] = [$"Share duration must be between {ShareDocumentVersionRequest.Minimum} and {ShareDocumentVersionRequest.Maximum} minutes."]
            });
        }

        var lifetime = TimeSpan.FromMinutes(normalizedMinutes);
        var linkResult = await fileAccess.GetDownloadLinkAsync(version.StorageKey, lifetime, cancellationToken);
        if (linkResult.IsFailure || linkResult.Value is null)
        {
            return MapFileErrors(linkResult.Errors);
        }

        var shareLink = new DocumentShareLinkResponse(linkResult.Value.Uri, linkResult.Value.ExpiresAtUtc, shareRequest.IsPublic);
        return TypedResults.Ok(shareLink);
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
            });
        }

        var normalizedFit = string.IsNullOrWhiteSpace(fit)
            ? "cover"
            : fit.Trim().ToLowerInvariant();

        if (normalizedFit != "cover" && normalizedFit != "contain")
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["fit"] = ["Parameter 'fit' must be either 'cover' or 'contain'."]
            });
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
            });
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

    private static DocumentResponse MapDocument(DomainDocument document)
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

        var tags = document.Tags
            .Select(documentTag =>
            {
                var tag = documentTag.Tag;
                var path = tag?.Path ?? string.Empty;
                var displayName = !string.IsNullOrWhiteSpace(path)
                    ? path.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).LastOrDefault() ?? path
                    : tag?.Slug ?? documentTag.TagId.ToString();

                return new DocumentTagResponse(
                    documentTag.TagId,
                    tag?.NamespaceSlug ?? string.Empty,
                    tag?.Slug ?? string.Empty,
                    path,
                    tag?.IsActive ?? false,
                    displayName,
                    documentTag.AppliedBy,
                    documentTag.AppliedAtUtc);
            })
            .OrderBy(tag => tag.NamespaceSlug)
            .ThenBy(tag => tag.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

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
            versionResponse,
            tags);
    }

    private static IOrderedQueryable<DomainDocument> ApplySorting(IQueryable<DomainDocument> source, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return source.OrderByDescending(document => document.UpdatedAtUtc);
        }

        var parts = sort.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return source.OrderByDescending(document => document.UpdatedAtUtc);
        }

        var field = parts[0].ToLowerInvariant();
        var direction = parts.Length > 1 ? parts[1].ToLowerInvariant() : "asc";

        return (field, direction) switch
        {
            ("name", "desc") or ("title", "desc") => source.OrderByDescending(document => document.Title.Value),
            ("name", _) or ("title", _) => source.OrderBy(document => document.Title.Value),
            ("modified", "asc") or ("updated", "asc") or ("updated_at", "asc") => source.OrderBy(document => document.UpdatedAtUtc),
            ("modified", _) or ("updated", _) or ("updated_at", _) => source.OrderByDescending(document => document.UpdatedAtUtc),
            ("created", "desc") or ("created_at", "desc") => source.OrderByDescending(document => document.CreatedAtUtc),
            ("created", _) or ("created_at", _) => source.OrderBy(document => document.CreatedAtUtc),
            ("size", "desc") => source.OrderByDescending(document => document.Versions
                .OrderByDescending(version => version.VersionNo)
                .Select(version => (long?)version.Bytes)
                .FirstOrDefault()),
            ("size", _) => source.OrderBy(document => document.Versions
                .OrderByDescending(version => version.VersionNo)
                .Select(version => (long?)version.Bytes)
                .FirstOrDefault()),
            _ => source.OrderByDescending(document => document.UpdatedAtUtc),
        };
    }
}
