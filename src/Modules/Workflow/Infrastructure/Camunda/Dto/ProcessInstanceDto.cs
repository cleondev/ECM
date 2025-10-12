using System.Text.Json.Serialization;

namespace ECM.Workflow.Infrastructure.Camunda.Dto;

internal sealed record ProcessInstanceDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("definitionId")] string DefinitionId,
    [property: JsonPropertyName("businessKey")] string? BusinessKey,
    [property: JsonPropertyName("caseInstanceId")] string? CaseInstanceId,
    [property: JsonPropertyName("ended")] bool? Ended,
    [property: JsonPropertyName("suspended")] bool? Suspended,
    [property: JsonPropertyName("tenantId")] string? TenantId);
