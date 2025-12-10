using Ecm.Sdk.Authentication;
using Ecm.Sdk.Clients;
using Ecm.Sdk.Models.Documents;
using Ecm.Sdk.Models.Tags;

using Microsoft.Extensions.Options;

using Tagger.Configuration;
using Tagger.Rules.Configuration;

namespace Tagger.Services;

internal interface IDocumentTagAssignmentService
{
    /// <summary>
    /// Applies tag IDs and tag names (creating them if needed) to a document and returns how many assignments were made.
    /// </summary>
    Task<int> AssignTagsAsync(
        Guid documentId,
        IReadOnlyCollection<Guid> tagIds,
        IReadOnlyCollection<string> tagNames,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Uses the ECM SDK to ensure target namespaces exist and to assign tags produced by the rule engine to documents.
/// </summary>
internal sealed class DocumentTagAssignmentService : IDocumentTagAssignmentService
{
    private readonly EcmFileClient _client;
    private readonly ILogger<DocumentTagAssignmentService> _logger;
    private readonly IOptionsMonitor<TaggerRulesOptions> _options;
    private readonly IOptionsMonitor<EcmUserOptions> _userOptions;
    private readonly SemaphoreSlim _userIdLock = new(1, 1);

    private static readonly string[] TagPathSegments = ["LOS", "CreditApplication"];

    private Guid? _cachedUserId;
    private bool _userIdResolved;

    public DocumentTagAssignmentService(
        EcmFileClient client,
        ILogger<DocumentTagAssignmentService> logger,
        IOptionsMonitor<TaggerRulesOptions> options,
        IOptionsMonitor<EcmUserOptions> userOptions)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _userOptions = userOptions ?? throw new ArgumentNullException(nameof(userOptions));
    }

    public async Task<int> AssignTagsAsync(
        Guid documentId,
        IReadOnlyCollection<Guid> tagIds,
        IReadOnlyCollection<string> tagNames,
        CancellationToken cancellationToken = default)
    {
        EnsureUserContext();

        if ((tagIds is null || tagIds.Count == 0) && (tagNames is null || tagNames.Count == 0))
        {
            return 0;
        }

        var appliedBy = await ResolveAppliedByAsync(cancellationToken).ConfigureAwait(false);
        var appliedCount = 0;

        var document = await _client.GetDocumentAsync(documentId, cancellationToken).ConfigureAwait(false);
        if (document is null)
        {
            _logger.LogWarning("Document {DocumentId} was not found when applying the tag.", documentId);
            return 0;
        }

        var seen = document.Tags is null
            ? new HashSet<Guid>()
            : document.Tags.Select(tag => tag.Id).ToHashSet();

        var targetNamespace = await GetOrCreateTargetNamespaceAsync(document, appliedBy, cancellationToken)
            .ConfigureAwait(false);

        var namespaceTags = targetNamespace is not null
            ? await LoadNamespaceTagsAsync(targetNamespace.Id, cancellationToken).ConfigureAwait(false)
            : new List<TagLabelDto>();

        var tagParentId = targetNamespace is null
            ? null
            : await EnsureTagPathAsync(targetNamespace.Id, namespaceTags, appliedBy, cancellationToken)
                .ConfigureAwait(false);

        foreach (var tagId in tagIds.Where(tagId => tagId != Guid.Empty))
        {
            if (!seen.Add(tagId))
            {
                continue;
            }

            var result = await _client.AssignTagToDocumentAsync(documentId, tagId, appliedBy, cancellationToken)
                .ConfigureAwait(false);

            if (!result)
            {
                _logger.LogWarning(
                    "Skipping tag {TagId} for document {DocumentId}: assignment failed.",
                    tagId,
                    documentId);
                continue;
            }

            appliedCount++;
        }

        if (tagNames is not null && tagNames.Count > 0)
        {
            if (targetNamespace is null || tagParentId is null)
            {
                _logger.LogWarning(
                    "Unable to create tags for document {DocumentId} because no target namespace or parent path was resolved.",
                    documentId);
            }
            else
            {
                foreach (var tagName in tagNames)
                {
                    var tagId = await ResolveTagIdAsync(
                            tagName,
                            targetNamespace.Id,
                            tagParentId.Value,
                            namespaceTags,
                            appliedBy,
                            cancellationToken)
                        .ConfigureAwait(false);

                    if (tagId is null || tagId == Guid.Empty || !seen.Add(tagId.Value))
                    {
                        continue;
                    }

                    var assigned = await _client.AssignTagToDocumentAsync(documentId, tagId.Value, appliedBy, cancellationToken)
                        .ConfigureAwait(false);

                    if (!assigned)
                    {
                        _logger.LogWarning(
                            "Skipping tag {TagName} for document {DocumentId}: assignment failed.",
                            tagName,
                            documentId);
                        continue;
                    }

                    appliedCount++;
                }
            }
        }

        return appliedCount;
    }

    private async Task<TagNamespaceDto?> GetOrCreateTargetNamespaceAsync(
        DocumentDto document,
        Guid? createdBy,
        CancellationToken cancellationToken)
    {
        var targetGroupId = document.GroupId;

        if (targetGroupId is null || targetGroupId == Guid.Empty)
        {
            var firstGroup = document.GroupIds.FirstOrDefault();
            if (firstGroup != Guid.Empty)
            {
                targetGroupId = firstGroup;
            }
        }

        if (targetGroupId is null || targetGroupId == Guid.Empty)
        {
            _logger.LogWarning(
               "Unable to determine the group for document {DocumentId}; skipping tag creation.",
                document.Id);
            return null;
        }

        var namespaces = await _client.ListTagNamespacesAsync("group", cancellationToken).ConfigureAwait(false);
        var existing = namespaces.FirstOrDefault(ns => ns.OwnerGroupId == targetGroupId);
        if (existing is not null)
        {
            return existing;
        }

        var created = await _client.CreateTagNamespaceAsync(
                new TagNamespaceCreateRequest(
                    Scope: "group",
                    DisplayName: "LOS",
                    OwnerGroupId: targetGroupId,
                    OwnerUserId: null,
                    CreatedBy: createdBy),
                cancellationToken)
            .ConfigureAwait(false);

        if (created is null)
        {
            _logger.LogWarning(
               "Unable to create namespace for group {GroupId} when tagging document {DocumentId}.",
                targetGroupId,
                document.Id);
        }

        return created;
    }

    private async Task<List<TagLabelDto>> LoadNamespaceTagsAsync(Guid namespaceId, CancellationToken cancellationToken)
    {
        var tags = await _client.ListManagedTagsAsync("group", cancellationToken).ConfigureAwait(false);
        return tags.Where(tag => tag.NamespaceId == namespaceId).ToList();
    }

    private async Task<Guid?> EnsureTagPathAsync(
        Guid namespaceId,
        List<TagLabelDto> namespaceTags,
        Guid? createdBy,
        CancellationToken cancellationToken)
    {
        Guid? parentId = null;

        foreach (var segment in TagPathSegments)
        {
            var existing = FindTag(namespaceTags, segment, parentId);
            if (existing is not null)
            {
                parentId = existing.Id;
                continue;
            }

            var created = await _client.CreateManagedTagAsync(
                    new TagCreateRequest(
                        NamespaceId: namespaceId,
                        ParentId: parentId,
                        Name: segment,
                        SortOrder: null,
                        Color: null,
                        IconKey: null,
                        CreatedBy: createdBy,
                        IsSystem: true),
                    cancellationToken)
                .ConfigureAwait(false);

            if (created is null)
            {
                _logger.LogWarning(
                    "Unable to create path segment tag {Segment} in namespace {NamespaceId}.",
                    segment,
                    namespaceId);
                return null;
            }

            namespaceTags.Add(created);
            parentId = created.Id;
        }

        return parentId;
    }

    private async Task<Guid?> ResolveTagIdAsync(
        string? tagName,
        Guid namespaceId,
        Guid parentId,
        List<TagLabelDto> namespaceTags,
        Guid? createdBy,
        CancellationToken cancellationToken)
    {
        var normalized = tagName?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var existing = FindTag(namespaceTags, normalized!, parentId);
        if (existing is not null)
        {
            return existing.Id;
        }

        var created = await _client.CreateManagedTagAsync(
                new TagCreateRequest(
                    NamespaceId: namespaceId,
                    ParentId: parentId,
                    Name: normalized!,
                    SortOrder: null,
                    Color: null,
                    IconKey: null,
                    CreatedBy: createdBy,
                    IsSystem: true),
                cancellationToken)
            .ConfigureAwait(false);

        if (created is null)
        {
            _logger.LogWarning("Failed to create managed tag {TagName}.", normalized);
            return null;
        }

        namespaceTags.Add(created);
        return created.Id;
    }

    private static TagLabelDto? FindTag(IEnumerable<TagLabelDto> tags, string name, Guid? parentId)
        => tags.FirstOrDefault(tag =>
            string.Equals(tag.Name, name, StringComparison.OrdinalIgnoreCase)
            && tag.ParentId == parentId);

    private async Task<Guid?> ResolveAppliedByAsync(CancellationToken cancellationToken)
    {
        if (_options.CurrentValue.AppliedBy is { } appliedBy)
        {
            return appliedBy;
        }

        var userKey = _userOptions.CurrentValue.UserKey;

        if (Guid.TryParse(userKey, out var userId))
        {
            return userId;
        }

        if (_userIdResolved)
        {
            return _cachedUserId;
        }

        await _userIdLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_userIdResolved)
            {
                return _cachedUserId;
            }

            var profile = await _client.GetCurrentUserProfileAsync(cancellationToken).ConfigureAwait(false);
            _cachedUserId = profile?.Id;
            _userIdResolved = true;

            if (_cachedUserId is null && !string.IsNullOrWhiteSpace(userKey))
            {
                _logger.LogDebug(
                    "Failed to resolve user id for configured EcmUser:UserKey; tag creations will be recorded as automated.");
            }

            return _cachedUserId;
        }
        finally
        {
            _userIdLock.Release();
        }
    }

    private void EnsureUserContext()
    {
        var userKey = _userOptions.CurrentValue.UserKey;
        userKey = string.IsNullOrEmpty(userKey) ? "system@local" : userKey;
        if (!string.IsNullOrWhiteSpace(userKey))
        {
            ManualEcmUserContext.SetUserKey(userKey);
            return;
        }

        if (ManualEcmUserContext.HasUserKey)
        {
            return;
        }

        throw new InvalidOperationException("No ECM user identity configured. Provide EcmUser:UserKey or call ManualEcmUserContext.SetUserKey before assigning tags.");
    }
}
