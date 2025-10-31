using System;

namespace AppGateway.Contracts.Documents;

public sealed record DocumentShareLinkDto(
    Uri Url,
    Uri ShortUrl,
    DateTimeOffset ExpiresAtUtc,
    bool IsPublic,
    bool RequiresPassword,
    string SubjectType,
    Guid? SubjectId);
