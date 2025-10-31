using System;

namespace AppGateway.Contracts.Documents;

public sealed class UpdateDocumentRequestDto
{
    private Guid? _groupId;

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
}
