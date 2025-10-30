using System.Text.Json.Serialization;

namespace ECM.Workflow.Infrastructure.Camunda.Dto;

internal sealed record TaskAssigneeRequestDto([property: JsonPropertyName("userId")] string UserId);
