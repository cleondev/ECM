using System.Text.Json.Serialization;

namespace ECM.Workflow.Infrastructure.Camunda.Dto;

internal sealed record TaskClaimRequestDto([property: JsonPropertyName("userId")] string UserId);
