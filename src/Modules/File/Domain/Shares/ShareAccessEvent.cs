using System.Net;

namespace ECM.File.Domain.Shares;

public sealed record ShareAccessEvent(
    Guid ShareId,
    DateTimeOffset OccurredAt,
    string Action,
    bool Succeeded,
    IPAddress? RemoteIp,
    string? UserAgent);
