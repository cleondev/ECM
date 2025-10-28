using System.Linq;
using System.Text.Json;
using ECM.Ocr.Application.Abstractions;
using ECM.Ocr.Application.Commands;
using ECM.Ocr.Application.Models;
using ECM.Ocr.Domain.Annotations;
using ECM.Ocr.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECM.Ocr.Infrastructure.Persistence;

internal sealed class DatabaseOcrProvider : IOcrProvider
{
    private readonly OcrDbContext _dbContext;
    private readonly ILogger<DatabaseOcrProvider> _logger;

    public DatabaseOcrProvider(OcrDbContext dbContext, ILogger<DatabaseOcrProvider> logger)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<StartOcrResult> StartProcessingAsync(StartOcrCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        _logger.LogInformation(
            "OCR processing request for document {DocumentId} acknowledged with database-backed provider.",
            command.DocumentId);

        return Task.FromResult(StartOcrResult.Empty);
    }

    public async Task<OcrResult> GetSampleResultAsync(string sampleId, CancellationToken cancellationToken = default)
    {
        var versionId = ParseGuid(sampleId, nameof(sampleId));

        var result = await _dbContext.Results
            .AsNoTracking()
            .Include(result => result.PageTexts)
            .Include(result => result.Extractions)
            .Include(result => result.Annotations)
            .FirstOrDefaultAsync(result => result.VersionId == versionId, cancellationToken)
            .ConfigureAwait(false);

        if (result is null)
        {
            return OcrResult.Empty;
        }

        var payload = new
        {
            documentId = result.DocumentId,
            versionId = result.VersionId,
            pages = result.Pages,
            lang = result.Lang,
            summary = result.Summary,
            createdAt = result.CreatedAt,
            pageTexts = result.PageTexts
                .OrderBy(page => page.PageNo)
                .Select(page => new { pageNo = page.PageNo, content = page.Content }),
            extractions = result.Extractions
                .OrderBy(extraction => extraction.FieldKey)
                .Select(extraction => new
                {
                    fieldKey = extraction.FieldKey,
                    valueText = extraction.ValueText,
                    confidence = extraction.Confidence,
                    provenance = CloneJson(extraction.Provenance)
                }),
            annotations = result.Annotations
                .OrderBy(annotation => annotation.CreatedAt)
                .Select(ToAnnotationPayload)
        };

        var json = JsonSerializer.SerializeToElement(payload);
        return new OcrResult(json);
    }

    public async Task<OcrResult> GetBoxingResultAsync(string sampleId, string boxingId, CancellationToken cancellationToken = default)
    {
        var versionId = ParseGuid(sampleId, nameof(sampleId));
        var annotationId = ParseGuid(boxingId, nameof(boxingId));

        var annotation = await _dbContext.Annotations
            .AsNoTracking()
            .FirstOrDefaultAsync(
                entity => entity.VersionId == versionId && entity.Id == annotationId,
                cancellationToken)
            .ConfigureAwait(false);

        if (annotation is null)
        {
            return OcrResult.Empty;
        }

        var payload = ToAnnotationPayload(annotation);
        var json = JsonSerializer.SerializeToElement(payload);
        return new OcrResult(json);
    }

    public async Task<OcrBoxesResult> ListBoxesAsync(string sampleId, CancellationToken cancellationToken = default)
    {
        var versionId = ParseGuid(sampleId, nameof(sampleId));

        var annotations = await _dbContext.Annotations
            .AsNoTracking()
            .Where(annotation => annotation.VersionId == versionId)
            .OrderBy(annotation => annotation.CreatedAt)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        if (annotations.Count == 0)
        {
            return OcrBoxesResult.Empty;
        }

        var payload = annotations.Select(ToAnnotationPayload);
        var json = JsonSerializer.SerializeToElement(payload);
        return new OcrBoxesResult(json);
    }

    public async Task SetBoxValueAsync(string sampleId, string boxId, string value, CancellationToken cancellationToken = default)
    {
        var versionId = ParseGuid(sampleId, nameof(sampleId));
        var annotationId = ParseGuid(boxId, nameof(boxId));
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        var annotation = await _dbContext.Annotations
            .FirstOrDefaultAsync(
                entity => entity.VersionId == versionId && entity.Id == annotationId,
                cancellationToken)
            .ConfigureAwait(false);

        if (annotation is null)
        {
            _logger.LogWarning(
                "Attempted to update OCR annotation {AnnotationId} for version {VersionId} but it was not found.",
                annotationId,
                versionId);
            return;
        }

        annotation.SetValue(value);

        if (!string.IsNullOrWhiteSpace(annotation.FieldKey))
        {
            var extraction = await _dbContext.Extractions
                .FirstOrDefaultAsync(
                    entity => entity.VersionId == versionId && entity.DocumentId == annotation.DocumentId && entity.FieldKey == annotation.FieldKey,
                    cancellationToken)
                .ConfigureAwait(false);

            extraction?.SetValue(value);
        }

        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private static Guid ParseGuid(string value, string parameterName)
    {
        if (!Guid.TryParse(value, out var guid))
        {
            throw new ArgumentException($"The value '{value}' is not a valid GUID.", parameterName);
        }

        return guid;
    }

    private static object? CloneJson(JsonDocument? document)
    {
        return document is null ? null : document.RootElement.Clone();
    }

    private static object ToAnnotationPayload(OcrAnnotation annotation)
    {
        return new
        {
            id = annotation.Id,
            documentId = annotation.DocumentId,
            versionId = annotation.VersionId,
            templateId = annotation.TemplateId,
            fieldKey = annotation.FieldKey,
            valueText = annotation.ValueText,
            bbox = CloneJson(annotation.BoundingBox),
            confidence = annotation.Confidence,
            source = annotation.Source,
            createdBy = annotation.CreatedBy,
            createdAt = annotation.CreatedAt
        };
    }
}
