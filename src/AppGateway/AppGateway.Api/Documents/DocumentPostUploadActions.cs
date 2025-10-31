using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Contracts.Tags;
using AppGateway.Contracts.Workflows;
using AppGateway.Infrastructure.Ecm;
using Microsoft.Extensions.Logging;

namespace AppGateway.Api.Documents;

internal static class DocumentPostUploadActions
{
    public static async Task AssignTagsAsync(
        IEcmApiClient client,
        ILogger logger,
        Guid documentId,
        IReadOnlyCollection<Guid> tagIds,
        Guid appliedBy,
        CancellationToken cancellationToken)
    {
        if (tagIds.Count == 0)
        {
            return;
        }

        foreach (var tagId in tagIds)
        {
            try
            {
                var assigned = await client.AssignTagToDocumentAsync(
                    documentId,
                    new AssignTagRequestDto(tagId, appliedBy),
                    cancellationToken);

                if (!assigned)
                {
                    logger.LogWarning("Failed to assign tag {TagId} to document {DocumentId}", tagId, documentId);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Failed to assign tag {TagId} to document {DocumentId}", tagId, documentId);
            }
        }
    }

    public static async Task StartWorkflowAsync(
        IEcmApiClient client,
        ILogger logger,
        Guid documentId,
        string? flowDefinition,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(flowDefinition))
        {
            return;
        }

        try
        {
            await client.StartWorkflowAsync(new StartWorkflowRequestDto
            {
                DocumentId = documentId,
                Definition = flowDefinition,
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to start workflow {Workflow} for document {DocumentId}",
                flowDefinition,
                documentId);
        }
    }
}
