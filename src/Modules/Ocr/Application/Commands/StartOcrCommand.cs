using System;
using System.Collections.Generic;

namespace ECM.Ocr.Application.Commands;

public sealed record StartOcrCommand(
    Guid DocumentId,
    string Title,
    string? Summary,
    string? Content,
    IReadOnlyDictionary<string, string>? Metadata,
    IReadOnlyCollection<string>? Tags,
    Uri FileUrl);
