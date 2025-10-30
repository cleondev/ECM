using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Tags.Repositories;
using ECM.Document.Application.Tags.Results;
using ECM.Document.Domain.Tags;

namespace ECM.Document.Application.Tags.Commands;

public sealed class CreateTagLabelCommandHandler(
    ITagLabelRepository tagLabelRepository,
    ITagNamespaceRepository tagNamespaceRepository,
    ISystemClock clock)
{
    private static readonly Regex SlugPattern = new("^[a-z0-9_]+(-[a-z0-9_]+)*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex PathPattern = new(
        "^[a-z0-9_]+(?:-[a-z0-9_]+)*(?:/[a-z0-9_]+(?:-[a-z0-9_]+)*)*$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ITagLabelRepository _tagLabelRepository = tagLabelRepository;
    private readonly ITagNamespaceRepository _tagNamespaceRepository = tagNamespaceRepository;
    private readonly ISystemClock _clock = clock;

    public async Task<OperationResult<TagLabelResult>> HandleAsync(CreateTagLabelCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.NamespaceSlug))
        {
            return OperationResult<TagLabelResult>.Failure("Namespace slug is required.");
        }

        if (string.IsNullOrWhiteSpace(command.Slug))
        {
            return OperationResult<TagLabelResult>.Failure("Tag slug is required.");
        }

        var namespaceSlug = command.NamespaceSlug.Trim().ToLowerInvariant();
        var slug = command.Slug.Trim().ToLowerInvariant();
        var path = string.IsNullOrWhiteSpace(command.Path)
            ? slug
            : command.Path.Trim().ToLowerInvariant();

        if (!SlugPattern.IsMatch(slug))
        {
            return OperationResult<TagLabelResult>.Failure("Slug must contain lowercase letters, numbers, underscores or hyphens.");
        }

        if (!PathPattern.IsMatch(path))
        {
            return OperationResult<TagLabelResult>.Failure(
                "Path must contain lowercase letters, numbers, underscores, hyphens, or forward slashes.");
        }

        if (!await EnsureNamespaceExistsAsync(namespaceSlug, command.CreatedBy, cancellationToken).ConfigureAwait(false))
        {
            return OperationResult<TagLabelResult>.Failure("Tag namespace does not exist.");
        }

        var existingTag = await _tagLabelRepository
            .GetByNamespaceAndPathAsync(namespaceSlug, path, cancellationToken)
            .ConfigureAwait(false);

        if (existingTag is not null)
        {
            return OperationResult<TagLabelResult>.Failure("Tag with the provided namespace and path already exists.");
        }

        var createdAt = _clock.UtcNow;
        var tagLabel = TagLabel.Create(namespaceSlug, slug, path, command.CreatedBy, createdAt);
        await _tagLabelRepository.AddAsync(tagLabel, cancellationToken).ConfigureAwait(false);

        var result = new TagLabelResult(
            tagLabel.Id,
            tagLabel.NamespaceSlug,
            tagLabel.Slug,
            tagLabel.Path,
            tagLabel.IsActive,
            tagLabel.CreatedBy,
            tagLabel.CreatedAtUtc);

        return OperationResult<TagLabelResult>.Success(result);
    }

    private async Task<bool> EnsureNamespaceExistsAsync(
        string namespaceSlug,
        Guid? ownerUserId,
        CancellationToken cancellationToken)
    {
        if (await _tagNamespaceRepository.ExistsAsync(namespaceSlug, cancellationToken).ConfigureAwait(false))
        {
            return true;
        }

        if (!TryGetUserNamespaceDisplayName(namespaceSlug, out var displayName))
        {
            return false;
        }

        var createdAt = _clock.UtcNow;
        await _tagNamespaceRepository
            .EnsureUserNamespaceAsync(namespaceSlug, ownerUserId, displayName, createdAt, cancellationToken)
            .ConfigureAwait(false);

        return await _tagNamespaceRepository.ExistsAsync(namespaceSlug, cancellationToken).ConfigureAwait(false);
    }

    private static bool TryGetUserNamespaceDisplayName(string namespaceSlug, out string? displayName)
    {
        const string Prefix = "user/";
        if (namespaceSlug.StartsWith(Prefix, StringComparison.Ordinal) && namespaceSlug.Length > Prefix.Length)
        {
            displayName = namespaceSlug[Prefix.Length..];
            return true;
        }

        displayName = null;
        return false;
    }
}
