using Ecm.Sdk.Authentication;
using Ecm.Sdk.Clients;
using Ecm.Sdk.Models.Tags;

using Microsoft.Extensions.Options;

namespace Tagger;

internal interface IDocumentTagAssignmentService
{
    Task<int> AssignTagsAsync(
        Guid documentId,
        IReadOnlyCollection<Guid> tagIds,
        IReadOnlyCollection<string> tagNames,
        CancellationToken cancellationToken = default);
}

internal sealed class DocumentTagAssignmentService : IDocumentTagAssignmentService
{
    private readonly EcmFileClient _client;
    private readonly ILogger<DocumentTagAssignmentService> _logger;
    private readonly IOptionsMonitor<TaggingRulesOptions> _options;
    private readonly IOptionsMonitor<EcmUserOptions> _userOptions;

    public DocumentTagAssignmentService(
        EcmFileClient client,
        ILogger<DocumentTagAssignmentService> logger,
        IOptionsMonitor<TaggingRulesOptions> options,
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

        var appliedBy = _options.CurrentValue.AppliedBy;
        var appliedCount = 0;
        var seen = new HashSet<Guid>();

        var tagLookup = await BuildTagLookupAsync(tagNames, cancellationToken).ConfigureAwait(false);

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
            foreach (var tagName in tagNames)
            {
                var tagId = await ResolveTagIdAsync(tagName, tagLookup, cancellationToken).ConfigureAwait(false);

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

        return appliedCount;
    }

    private async Task<Dictionary<string, TagLabelDto>> BuildTagLookupAsync(
        IReadOnlyCollection<string>? tagNames,
        CancellationToken cancellationToken)
    {
        if (tagNames is null || tagNames.Count == 0)
        {
            return new Dictionary<string, TagLabelDto>(StringComparer.OrdinalIgnoreCase);
        }

        var existing = await _client.ListTagsAsync(cancellationToken).ConfigureAwait(false);
        return existing.ToDictionary(tag => tag.Name, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<Guid?> ResolveTagIdAsync(
        string? tagName,
        IDictionary<string, TagLabelDto> tagLookup,
        CancellationToken cancellationToken)
    {
        var normalized = tagName?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        if (tagLookup.TryGetValue(normalized, out var existing))
        {
            return existing.Id;
        }

        var createdBy = _options.CurrentValue.AppliedBy;
        if (createdBy is null)
        {
            _logger.LogWarning(
                "Cannot create tag {TagName} because AppliedBy is not configured.",
                normalized);
            return null;
        }

        var created = await _client.CreateTagAsync(
            new TagCreateRequest(
                NamespaceId: null,
                ParentId: null,
                Name: normalized,
                SortOrder: null,
                Color: null,
                IconKey: null,
                CreatedBy: createdBy,
                IsSystem: true),
            cancellationToken)
            .ConfigureAwait(false);

        if (created is null)
        {
            _logger.LogWarning("Failed to create tag {TagName}.", normalized);
            return null;
        }

        tagLookup[created.Name] = created;
        return created.Id;
    }

    private void EnsureUserContext()
    {
        var userKey = _userOptions.CurrentValue.UserKey;

        if (!string.IsNullOrWhiteSpace(userKey))
        {
            ManualEcmUserContext.SetUserKey(userKey);
            return;
        }

        if (ManualEcmUserContext.HasUserKey)
        {
            return;
        }

        throw new InvalidOperationException(
            "No ECM user identity configured. Provide EcmUser:UserKey or call ManualEcmUserContext.SetUserKey before assigning tags.");
    }
}
