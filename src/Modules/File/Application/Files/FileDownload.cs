using System;

namespace ECM.File.Application.Files;

public sealed record FileDownload(byte[] Content, string ContentType, string? FileName, DateTimeOffset? LastModifiedUtc);
