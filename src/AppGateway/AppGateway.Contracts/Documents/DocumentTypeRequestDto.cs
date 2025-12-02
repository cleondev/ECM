namespace AppGateway.Contracts.Documents;

public sealed record DocumentTypeRequestDto(
    string TypeKey,
    string TypeName,
    string? Description,
    bool? IsActive);
