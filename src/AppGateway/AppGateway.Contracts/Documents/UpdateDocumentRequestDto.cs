using System;

namespace AppGateway.Contracts.Documents;

public sealed class UpdateDocumentRequestDto
{
    private Guid? _groupId;
    private Guid? _documentTypeId;

    public string? Title { get; init; }

    public string? Status { get; init; }

    public string? Sensitivity { get; init; }

    public Guid? GroupId
    {
        get => _groupId;
        init
        {
            _groupId = value;
            HasGroupId = true;
        }
    }

    public bool HasGroupId { get; private set; }

    public Guid? DocumentTypeId
    {
        get => _documentTypeId;
        init
        {
            _documentTypeId = value;
            HasDocumentTypeId = true;
        }
    }

    public bool HasDocumentTypeId { get; private set; }
}
