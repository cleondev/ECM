using System;

namespace AppGateway.Infrastructure.Ecm;

public sealed record DocumentFileContent(
    byte[] Content,
    string ContentType,
    string? FileName,
    DateTimeOffset? LastModifiedUtc,
    bool EnableRangeProcessing);
