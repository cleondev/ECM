using System.Text.Json.Serialization;

namespace ECM.Workflow.Infrastructure.Camunda.Dto;

internal sealed record CamundaVariableDto(
    [property: JsonPropertyName("value")] object? Value,
    [property: JsonPropertyName("type")] string? Type);
