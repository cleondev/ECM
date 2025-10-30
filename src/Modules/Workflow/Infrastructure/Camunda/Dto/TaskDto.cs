using System;
using System.Text.Json.Serialization;

namespace ECM.Workflow.Infrastructure.Camunda.Dto;

internal sealed record TaskDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("assignee")] string? Assignee,
    [property: JsonPropertyName("created")] DateTimeOffset? Created,
    [property: JsonPropertyName("formKey")] string? FormKey,
    [property: JsonPropertyName("processDefinitionId")] string ProcessDefinitionId,
    [property: JsonPropertyName("processInstanceId")] string ProcessInstanceId);
