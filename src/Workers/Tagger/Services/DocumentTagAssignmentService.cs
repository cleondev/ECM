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
    /// Applies tag IDs and tag definitions (creating them if needed) to a document and returns how many assignments were made.
    /// </summary>
    Task<int> AssignTagsAsync(
        Guid documentId,
        IReadOnlyCollection<Guid> tagIds,
        IReadOnlyCollection<TagDefinition> tagDefinitions,
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
        IReadOnlyCollection<TagDefinition> tagDefinitions,
        CancellationToken cancellationToken = default)
    {
        EnsureUserContext();

        var hasIds = tagIds is not null && tagIds.Count > 0;
        var hasDefinitions = tagDefinitions is not null && tagDefinitions.Count > 0;

        if (!hasIds && !hasDefinitions)
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

        var namespaceCache = new Dictionary<string, TagNamespaceDto?>(StringComparer.OrdinalIgnoreCase);
        var scopeTagCache = new Dictionary<string, List<TagLabelDto>>(StringComparer.OrdinalIgnoreCase);
        var namespaceTagsCache = new Dictionary<Guid, List<TagLabelDto>>();

        foreach (var tagId in (tagIds ?? Array.Empty<Guid>()).Where(tagId => tagId != Guid.Empty))
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

        if (!hasDefinitions)
        {
            return appliedCount;
        }

        var normalizedDefinitions = (tagDefinitions ?? Array.Empty<TagDefinition>())
            .Where(definition => definition is not null)
            .Distinct(TagDefinition.Comparer)
            .ToArray();

        foreach (var definition in normalizedDefinitions)
        {
            var targetNamespace = await GetOrCreateNamespaceAsync(
                    definition,
                    document,
                    appliedBy,
                    namespaceCache,
                    cancellationToken)
                .ConfigureAwait(false);

            if (targetNamespace is null)
            {
                continue;
            }

            var scopeTags = await GetScopeTagsAsync(definition.Scope, scopeTagCache, cancellationToken)
                .ConfigureAwait(false);

            if (!namespaceTagsCache.TryGetValue(targetNamespace.Id, out var namespaceTags))
            {
                namespaceTags = scopeTags.Where(tag => tag.NamespaceId == targetNamespace.Id).ToList();
                namespaceTagsCache[targetNamespace.Id] = namespaceTags;
            }

            var parentId = await EnsureTagPathAsync(
                    definition.PathSegments,
                    targetNamespace.Id,
                    namespaceTags,
                    appliedBy,
                    cancellationToken)
                .ConfigureAwait(false);

            if (parentId is null && definition.PathSegments.Count > 0)
            {
                continue;
            }

            var resolvedTagId = await EnsureTagAsync(
                    definition,
                    targetNamespace.Id,
                    parentId,
                    namespaceTags,
                    appliedBy,
                    cancellationToken)
                .ConfigureAwait(false);

            if (resolvedTagId is null || resolvedTagId == Guid.Empty || !seen.Add(resolvedTagId.Value))
            {
                continue;
            }

            var assigned = await _client.AssignTagToDocumentAsync(documentId, resolvedTagId.Value, appliedBy, cancellationToken)
                .ConfigureAwait(false);

            if (!assigned)
            {
                _logger.LogWarning(
                    "Skipping tag {TagName} for document {DocumentId}: assignment failed.",
                    definition.Name,
                    documentId);
                continue;
            }

            appliedCount++;
        }

        return appliedCount;
    }

    private async Task<TagNamespaceDto?> GetOrCreateNamespaceAsync(
        TagDefinition definition,
        DocumentDto document,
        Guid? createdBy,
        IDictionary<string, TagNamespaceDto?> cache,
        CancellationToken cancellationToken)
    {
        var scope = definition.Scope ?? TagDefaults.DefaultScope;
        Guid? ownerGroupId = null;
        Guid? ownerUserId = null;

        switch (scope)
        {
            case TagScope.Global:
                ownerUserId = null;
                ownerGroupId = null;
                break;
            case TagScope.User:
                var requestedOwnerId = definition.OwnerUserId
                    ?? document.LatestVersion?.CreatedBy
                    ?? document.CreatedBy;

                ownerUserId = await ResolveOwnerUserIdAsync(requestedOwnerId, createdBy, cancellationToken)
                    .ConfigureAwait(false);

                if (ownerUserId is null || ownerUserId == Guid.Empty)
                {
                    _logger.LogWarning(
                        "Unable to determine the user namespace for tag {TagName}; skipping creation.",
                        definition.Name);
                    cache[BuildNamespaceCacheKey(scope, ownerGroupId, ownerUserId, definition.NamespaceDisplayName)] = null;
                    return null;
                }

                break;
            default:
                ownerGroupId = ResolveGroupId(document, definition.OwnerGroupId);

                if (ownerGroupId is null || ownerGroupId == Guid.Empty)
                {
                    _logger.LogWarning(
                        "Unable to determine the group namespace for document {DocumentId}; skipping tag {TagName}.",
                        document.Id,
                        definition.Name);
                    cache[BuildNamespaceCacheKey(scope, ownerGroupId, ownerUserId, definition.NamespaceDisplayName)] = null;
                    return null;
                }

                break;
        }

        var cacheKey = BuildNamespaceCacheKey(scope, ownerGroupId, ownerUserId, definition.NamespaceDisplayName);

        if (cache.TryGetValue(cacheKey, out var cachedNamespace))
        {
            return cachedNamespace;
        }

        var namespaces = await _client.ListTagNamespacesAsync(scope, cancellationToken).ConfigureAwait(false);

        if (scope == TagScope.Global)
        {
            var globalNamespace = namespaces.FirstOrDefault();
            if (globalNamespace is not null)
            {
                cache[cacheKey] = globalNamespace;
                return globalNamespace;
            }
        }

        var existing = namespaces.FirstOrDefault(ns =>
            ns.OwnerGroupId == ownerGroupId
            && ns.OwnerUserId == ownerUserId
            && string.Equals(ns.DisplayName, definition.NamespaceDisplayName, StringComparison.OrdinalIgnoreCase))
            ?? namespaces.FirstOrDefault(ns => ns.OwnerGroupId == ownerGroupId && ns.OwnerUserId == ownerUserId);

        if (existing is not null)
        {
            cache[cacheKey] = existing;
            return existing;
        }

        var created = await _client.CreateTagNamespaceAsync(
                new TagNamespaceCreateRequest(
                    Scope: scope,
                    DisplayName: definition.NamespaceDisplayName,
                    OwnerGroupId: ownerGroupId,
                    OwnerUserId: ownerUserId,
                    CreatedBy: createdBy),
                cancellationToken)
            .ConfigureAwait(false);

        if (created is null)
        {
            _logger.LogWarning(
                "Unable to create namespace for scope {Scope} when tagging document {DocumentId}.",
                scope,
                document.Id);
        }

        cache[cacheKey] = created;
        return created;
    }

    private async Task<List<TagLabelDto>> GetScopeTagsAsync(
        string scope,
        IDictionary<string, List<TagLabelDto>> cache,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(scope, out var cached))
        {
            return cached;
        }

        var tags = await _client.ListManagedTagsAsync(scope, cancellationToken).ConfigureAwait(false);
        var list = tags.ToList();
        cache[scope] = list;
        return list;
    }

    private async Task<Guid?> EnsureTagPathAsync(
        IReadOnlyList<string> pathSegments,
        Guid namespaceId,
        List<TagLabelDto> namespaceTags,
        Guid? createdBy,
        CancellationToken cancellationToken)
    {
        Guid? parentId = null;

        foreach (var segment in pathSegments)
        {
            if (string.IsNullOrWhiteSpace(segment))
            {
                continue;
            }

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
                        Name: segment.Trim(),
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

    private async Task<Guid?> EnsureTagAsync(
        TagDefinition definition,
        Guid namespaceId,
        Guid? parentId,
        List<TagLabelDto> namespaceTags,
        Guid? createdBy,
        CancellationToken cancellationToken)
    {
        var existing = FindTag(namespaceTags, definition.Name, parentId);
        if (existing is not null)
        {
            return existing.Id;
        }

        var created = await _client.CreateManagedTagAsync(
                new TagCreateRequest(
                    NamespaceId: namespaceId,
                    ParentId: parentId,
                    Name: definition.Name,
                    SortOrder: null,
                    Color: definition.Color,
                    IconKey: definition.IconKey,
                    CreatedBy: createdBy,
                    IsSystem: true),
                cancellationToken)
            .ConfigureAwait(false);

        if (created is null)
        {
            _logger.LogWarning("Failed to create managed tag {TagName}.", definition.Name);
            return null;
        }

        namespaceTags.Add(created);
        return created.Id;
    }

    private static Guid? ResolveGroupId(DocumentDto document, Guid? explicitGroupId)
    {
        var targetGroupId = explicitGroupId;

        if (targetGroupId is null || targetGroupId == Guid.Empty)
        {
            targetGroupId = document.GroupId;
        }

        if (targetGroupId is null || targetGroupId == Guid.Empty)
        {
            var firstGroup = document.GroupIds.FirstOrDefault();
            if (firstGroup != Guid.Empty)
            {
                targetGroupId = firstGroup;
            }
        }

        return targetGroupId == Guid.Empty ? null : targetGroupId;
    }

    private async Task<Guid?> ResolveOwnerUserIdAsync(
        Guid? requestedOwnerId,
        Guid? appliedBy,
        CancellationToken cancellationToken)
    {
        if (requestedOwnerId is not null && requestedOwnerId != Guid.Empty)
        {
            return requestedOwnerId;
        }

        if (appliedBy is not null && appliedBy != Guid.Empty)
        {
            return appliedBy;
        }

        return await ResolveAppliedByAsync(cancellationToken).ConfigureAwait(false);
    }

    private static string BuildNamespaceCacheKey(string scope, Guid? ownerGroupId, Guid? ownerUserId, string namespaceName)
    {
        return string.Join(
            "|",
            scope?.Trim().ToLowerInvariant() ?? string.Empty,
            ownerGroupId?.ToString() ?? string.Empty,
            ownerUserId?.ToString() ?? string.Empty,
            namespaceName ?? string.Empty);
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
