using System;

namespace ECM.Abstractions.Files;

public sealed record FileDownloadLink(Uri Uri, DateTimeOffset ExpiresAtUtc);
