using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ECM.Workflow.Infrastructure.Camunda.Dto;

internal sealed record TaskCompleteRequestDto(
    [property: JsonPropertyName("variables")] IDictionary<string, CamundaVariableDto> Variables);
