using System;
using System.Text.Json.Serialization;

namespace ECM.Workflow.Infrastructure.Camunda.Dto;

internal sealed record ProcessInstanceHistoryDto(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("startTime")] DateTimeOffset? StartTime,
    [property: JsonPropertyName("endTime")] DateTimeOffset? EndTime);
