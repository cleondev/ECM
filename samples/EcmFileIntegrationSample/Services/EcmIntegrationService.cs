using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecm.Sdk.Clients;
using Ecm.Sdk.Models.Documents;
using Ecm.Sdk.Models.Tags;

namespace EcmFileIntegrationSample.Services;

public interface IEcmIntegrationService
{
    Task<UserProfile?> GetProfileAsync(CancellationToken cancellationToken);

    Task<DocumentDto?> UploadDocumentAsync(DocumentUploadRequest uploadRequest, CancellationToken cancellationToken);

    Task<Uri?> GetDownloadUriAsync(Guid versionId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TagLabelDto>> ListTagsAsync(CancellationToken cancellationToken);

    Task<TagLabelDto?> CreateTagAsync(TagCreateRequest request, CancellationToken cancellationToken);

    Task<TagLabelDto?> UpdateTagAsync(Guid tagId, TagUpdateRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteTagAsync(Guid tagId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<TagLabelDto>> AssignTagsAsync(
        Guid documentId,
        IEnumerable<Guid> tagIds,
        Guid? appliedBy,
        IReadOnlyCollection<TagLabelDto>? knownTags,
        CancellationToken cancellationToken);

    Task<DocumentListResult?> ListDocumentsAsync(DocumentListQuery query, CancellationToken cancellationToken);

    Task<DocumentListItem?> UpdateDocumentAsync(Guid documentId, DocumentUpdateRequest request, CancellationToken cancellationToken);

    Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken);

    Task<bool> DeleteDocumentByVersionAsync(Guid versionId, CancellationToken cancellationToken);

    Task<DocumentDto?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken);

    Task<DocumentDownloadResult?> DownloadVersionAsync(Guid versionId, CancellationToken cancellationToken);
}

public sealed class EcmIntegrationService(EcmFileClient client) : IEcmIntegrationService
{
    private readonly EcmFileClient _client = client;

    public Task<UserProfile?> GetProfileAsync(CancellationToken cancellationToken)
    {
        return _client.GetCurrentUserProfileAsync(cancellationToken);
    }

    public Task<DocumentDto?> UploadDocumentAsync(DocumentUploadRequest uploadRequest, CancellationToken cancellationToken)
    {
        return _client.UploadDocumentAsync(uploadRequest, cancellationToken);
    }

    public Task<Uri?> GetDownloadUriAsync(Guid versionId, CancellationToken cancellationToken)
    {
        return _client.GetDownloadUriAsync(versionId, cancellationToken);
    }

    public Task<IReadOnlyCollection<TagLabelDto>> ListTagsAsync(CancellationToken cancellationToken)
    {
        return _client.ListTagsAsync(cancellationToken);
    }

    public Task<TagLabelDto?> CreateTagAsync(TagCreateRequest request, CancellationToken cancellationToken)
    {
        return _client.CreateTagAsync(request, cancellationToken);
    }

    public Task<TagLabelDto?> UpdateTagAsync(Guid tagId, TagUpdateRequest request, CancellationToken cancellationToken)
    {
        return _client.UpdateTagAsync(tagId, request, cancellationToken);
    }

    public Task<bool> DeleteTagAsync(Guid tagId, CancellationToken cancellationToken)
    {
        return _client.DeleteTagAsync(tagId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<TagLabelDto>> AssignTagsAsync(
        Guid documentId,
        IEnumerable<Guid> tagIds,
        Guid? appliedBy,
        IReadOnlyCollection<TagLabelDto>? knownTags,
        CancellationToken cancellationToken)
    {
        var tagLookup = new Dictionary<Guid, TagLabelDto>();
        if (knownTags is not null)
        {
            foreach (var tag in knownTags)
            {
                tagLookup[tag.Id] = tag;
            }
        }
        else
        {
            foreach (var tag in await _client.ListTagsAsync(cancellationToken))
            {
                tagLookup[tag.Id] = tag;
            }
        }

        var appliedTags = new List<TagLabelDto>();
        foreach (var tagId in tagIds.Distinct())
        {
            if (!tagLookup.TryGetValue(tagId, out var tag))
            {
                continue;
            }

            var assigned = await _client.AssignTagToDocumentAsync(documentId, tagId, appliedBy, cancellationToken);
            if (assigned)
            {
                appliedTags.Add(tag);
            }
        }

        return appliedTags;
    }

    public Task<DocumentListResult?> ListDocumentsAsync(DocumentListQuery query, CancellationToken cancellationToken)
    {
        return _client.ListDocumentsAsync(query, cancellationToken);
    }

    public Task<DocumentListItem?> UpdateDocumentAsync(Guid documentId, DocumentUpdateRequest request, CancellationToken cancellationToken)
    {
        return _client.UpdateDocumentAsync(documentId, request, cancellationToken);
    }

    public Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return _client.DeleteDocumentAsync(documentId, cancellationToken);
    }

    public Task<bool> DeleteDocumentByVersionAsync(Guid versionId, CancellationToken cancellationToken)
    {
        return _client.DeleteDocumentByVersionAsync(versionId, cancellationToken);
    }

    public Task<DocumentDto?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        return _client.GetDocumentAsync(documentId, cancellationToken);
    }

    public Task<DocumentDownloadResult?> DownloadVersionAsync(Guid versionId, CancellationToken cancellationToken)
    {
        return _client.DownloadVersionAsync(versionId, cancellationToken);
    }
}
