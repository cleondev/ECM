using System;

namespace ECM.Document.Api.Documents.Requests;

public sealed class UpdateDocumentRequest
{
    public string? Title { get; init; }

    public string? Status { get; init; }

    public string? Sensitivity { get; init; }

    public Guid? GroupId { get; init; }
}
