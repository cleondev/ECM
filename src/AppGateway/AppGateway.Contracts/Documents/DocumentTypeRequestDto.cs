using System.Text.Json;

namespace AppGateway.Contracts.Documents;

public sealed record DocumentTypeRequestDto(
    string TypeKey,
    string TypeName,
    string? Description,
    bool? IsActive,
    JsonDocument? Config);
