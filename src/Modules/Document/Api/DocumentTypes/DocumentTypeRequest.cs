using System;

namespace ECM.Document.Api.DocumentTypes;

public sealed record DocumentTypeRequest(
    string TypeKey,
    string TypeName,
    string? Description,
    bool? IsActive);
