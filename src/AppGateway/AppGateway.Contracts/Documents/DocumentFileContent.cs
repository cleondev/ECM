namespace AppGateway.Contracts.Documents;

using System;

public sealed record DocumentFileContent(
    byte[] Content,
    string ContentType,
    string? FileName,
    DateTimeOffset? LastModifiedUtc,
    bool EnableRangeProcessing);
