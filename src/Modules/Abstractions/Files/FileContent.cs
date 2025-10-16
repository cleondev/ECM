using System;

namespace ECM.Abstractions.Files;

public sealed record FileContent(byte[] Content, string ContentType, string? FileName, DateTimeOffset? LastModifiedUtc = null);
