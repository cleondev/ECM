using System;

namespace AppGateway.Contracts.Notifications;

public sealed record NotificationItemDto(
    string Id,
    string Title,
    string? Description,
    string Type,
    DateTimeOffset CreatedAt,
    bool IsRead,
    string? ActionUrl);
