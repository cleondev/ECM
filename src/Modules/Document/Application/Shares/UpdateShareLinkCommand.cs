using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Domain.Shares;
using Microsoft.Extensions.Logging;

namespace ECM.Document.Application.Shares;

public sealed record UpdateShareLinkCommand(
    Guid ShareId,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo,
    int? MaxViews,
    int? MaxDownloads,
    IReadOnlyCollection<SharePermission>? Permissions,
    string? Password,
    bool RemovePassword,
    string? FileName,
    string? FileExtension,
    string? FileContentType,
    long? FileSizeBytes,
    DateTimeOffset? FileCreatedAt,
    string? WatermarkJson,
    IReadOnlyCollection<string>? AllowedIps);

public sealed class UpdateShareLinkCommandHandler(
    IShareLinkRepository repository,
    ISharePasswordHasher passwordHasher,
    ISystemClock clock,
    ILogger<UpdateShareLinkCommandHandler> logger)
{
    private readonly IShareLinkRepository _repository = repository;
    private readonly ISharePasswordHasher _passwordHasher = passwordHasher;
    private readonly ISystemClock _clock = clock;
    private readonly ILogger<UpdateShareLinkCommandHandler> _logger = logger;

    public async Task<OperationResult> HandleAsync(UpdateShareLinkCommand command, CancellationToken cancellationToken = default)
    {
        if (command.ShareId == Guid.Empty)
        {
            return OperationResult.Failure("ShareIdRequired");
        }

        var share = await _repository.GetByIdAsync(command.ShareId, cancellationToken);
        if (share is null)
        {
            return OperationResult.Failure("ShareNotFound");
        }

        var now = _clock.UtcNow;
        var validFrom = command.ValidFrom ?? share.ValidFrom;
        var validTo = command.ValidTo ?? share.ValidTo;

        if (validTo.HasValue && validTo.Value < validFrom)
        {
            return OperationResult.Failure("InvalidValidityWindow");
        }

        share.UpdateWindow(validFrom, validTo);

        if (command.MaxViews is not null)
        {
            if (command.MaxViews < 0)
            {
                return OperationResult.Failure("MaxViewsInvalid");
            }

            share.UpdateQuotas(command.MaxViews, share.MaxDownloads);
        }

        if (command.MaxDownloads is not null)
        {
            if (command.MaxDownloads < 0)
            {
                return OperationResult.Failure("MaxDownloadsInvalid");
            }

            share.UpdateQuotas(share.MaxViews, command.MaxDownloads);
        }

        if (command.Permissions is not null)
        {
            var permissions = SharePermission.None;
            foreach (var permission in command.Permissions)
            {
                permissions |= permission;
            }

            share.UpdatePermissions(permissions);
        }

        if (command.RemovePassword)
        {
            share.UpdatePasswordHash(null);
        }
        else if (!string.IsNullOrWhiteSpace(command.Password))
        {
            share.UpdatePasswordHash(_passwordHasher.Hash(command.Password));
        }

        if (!string.IsNullOrWhiteSpace(command.FileName)
            || !string.IsNullOrWhiteSpace(command.FileContentType)
            || command.FileSizeBytes.HasValue
            || command.FileCreatedAt.HasValue)
        {
            share.UpdateFileMetadata(
                command.FileName ?? share.FileName,
                command.FileExtension ?? share.FileExtension,
                command.FileContentType ?? share.FileContentType,
                command.FileSizeBytes ?? share.FileSizeBytes,
                command.FileCreatedAt ?? share.FileCreatedAt);
        }

        if (command.WatermarkJson is not null)
        {
            share.UpdateWatermark(command.WatermarkJson);
        }

        if (command.AllowedIps is not null)
        {
            share.UpdateAllowedIps(ParseIps(command.AllowedIps));
        }

        await _repository.UpdateAsync(share, cancellationToken);

        _logger.LogInformation(
            "Share link {ShareId} updated at {Timestamp}.",
            share.Id,
            now);

        return OperationResult.Success();
    }

    private static IReadOnlyCollection<System.Net.IPAddress> ParseIps(IEnumerable<string> raw)
    {
        var list = new List<System.Net.IPAddress>();
        foreach (var value in raw)
        {
            if (System.Net.IPAddress.TryParse(value, out var ip))
            {
                list.Add(ip);
            }
        }

        return list;
    }
}
