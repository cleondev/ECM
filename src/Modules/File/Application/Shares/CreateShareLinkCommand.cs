using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.File.Domain.Shares;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Utilities.ShortCode;

namespace ECM.File.Application.Shares;

public sealed record CreateShareLinkCommand(
    Guid OwnerUserId,
    Guid DocumentId,
    Guid? VersionId,
    ShareSubjectType SubjectType,
    Guid? SubjectId,
    IReadOnlyCollection<SharePermission> Permissions,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo,
    int? MaxViews,
    int? MaxDownloads,
    string? Password,
    string FileName,
    string? FileExtension,
    string FileContentType,
    long FileSizeBytes,
    DateTimeOffset? FileCreatedAt,
    string? WatermarkJson,
    IReadOnlyCollection<string>? AllowedIps);

public sealed class CreateShareLinkCommandHandler(
    IShareLinkRepository repository,
    ISharePasswordHasher passwordHasher,
    ShortCodeGenerator codeGenerator,
    IOptions<ShareLinkOptions> options,
    ISystemClock clock,
    ILogger<CreateShareLinkCommandHandler> logger)
{
    private readonly IShareLinkRepository _repository = repository;
    private readonly ISharePasswordHasher _passwordHasher = passwordHasher;
    private readonly ShortCodeGenerator _codeGenerator = codeGenerator;
    private readonly ShareLinkOptions _options = options.Value;
    private readonly ISystemClock _clock = clock;
    private readonly ILogger<CreateShareLinkCommandHandler> _logger = logger;

    public async Task<OperationResult<ShareLinkDto>> HandleAsync(
        CreateShareLinkCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.OwnerUserId == Guid.Empty)
        {
            return OperationResult<ShareLinkDto>.Failure("OwnerRequired");
        }

        if (command.DocumentId == Guid.Empty)
        {
            return OperationResult<ShareLinkDto>.Failure("DocumentRequired");
        }

        if (command.SubjectType != ShareSubjectType.Public && (!command.SubjectId.HasValue || command.SubjectId == Guid.Empty))
        {
            return OperationResult<ShareLinkDto>.Failure("SubjectRequired");
        }

        if (string.IsNullOrWhiteSpace(command.FileName))
        {
            return OperationResult<ShareLinkDto>.Failure("FileNameRequired");
        }

        if (string.IsNullOrWhiteSpace(command.FileContentType))
        {
            return OperationResult<ShareLinkDto>.Failure("FileContentTypeRequired");
        }

        if (command.FileSizeBytes < 0)
        {
            return OperationResult<ShareLinkDto>.Failure("FileSizeInvalid");
        }

        var now = _clock.UtcNow;
        var validFrom = command.ValidFrom ?? now;
        var validTo = command.ValidTo;

        if (validTo.HasValue && validTo.Value < validFrom)
        {
            return OperationResult<ShareLinkDto>.Failure("InvalidValidityWindow");
        }

        if (command.MaxViews is < 0)
        {
            return OperationResult<ShareLinkDto>.Failure("MaxViewsInvalid");
        }

        if (command.MaxDownloads is < 0)
        {
            return OperationResult<ShareLinkDto>.Failure("MaxDownloadsInvalid");
        }

        var permissions = AggregatePermissions(command.Permissions);
        var passwordHash = string.IsNullOrWhiteSpace(command.Password)
            ? null
            : _passwordHasher.Hash(command.Password);

        var allowedIps = ParseIps(command.AllowedIps);
        var code = await GenerateUniqueCodeAsync(cancellationToken);

        var shareLink = new ShareLink(
            Guid.NewGuid(),
            code,
            command.OwnerUserId,
            command.DocumentId,
            command.VersionId,
            command.SubjectType,
            command.SubjectId,
            permissions,
            passwordHash,
            validFrom,
            validTo,
            command.MaxViews,
            command.MaxDownloads,
            command.FileName,
            command.FileExtension,
            command.FileContentType,
            command.FileSizeBytes,
            command.FileCreatedAt,
            command.WatermarkJson,
            allowedIps,
            now,
            revokedAt: null);

        await _repository.AddAsync(shareLink, cancellationToken);

        _logger.LogInformation(
            "Share link {ShareId} created for document {DocumentId} with code {Code}.",
            shareLink.Id,
            shareLink.DocumentId,
            shareLink.Code);

        var dto = ShareLinkMapper.ToDto(shareLink, _options);
        return OperationResult<ShareLinkDto>.Success(dto);
    }

    private async Task<string> GenerateUniqueCodeAsync(CancellationToken cancellationToken)
    {
        var min = Math.Max(6, _options.MinCodeLength);
        var max = Math.Max(min, _options.MaxCodeLength);

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var length = attempt switch
            {
                < 3 => min,
                < 6 => min + 1,
                _ => max,
            };

            var code = _codeGenerator.Generate(length);
            if (!await _repository.CodeExistsAsync(code, cancellationToken))
            {
                return code;
            }
        }

        // Final attempt with maximum length.
        while (true)
        {
            var code = _codeGenerator.Generate(Math.Max(min, max));
            if (!await _repository.CodeExistsAsync(code, cancellationToken))
            {
                return code;
            }
        }
    }

    private static SharePermission AggregatePermissions(IReadOnlyCollection<SharePermission> permissions)
    {
        if (permissions is null || permissions.Count == 0)
        {
            return SharePermission.View | SharePermission.Download;
        }

        var result = SharePermission.None;
        foreach (var permission in permissions)
        {
            result |= permission;
        }

        return result;
    }

    private static IReadOnlyCollection<System.Net.IPAddress> ParseIps(IReadOnlyCollection<string>? raw)
    {
        if (raw is null || raw.Count == 0)
        {
            return Array.Empty<System.Net.IPAddress>();
        }

        var list = new List<System.Net.IPAddress>(raw.Count);
        foreach (var candidate in raw)
        {
            if (System.Net.IPAddress.TryParse(candidate, out var ip))
            {
                list.Add(ip);
            }
        }

        return list;
    }
}
