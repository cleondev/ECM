using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ECM.Workflow.Infrastructure.Camunda.Dto;

internal sealed class StartProcessInstanceRequestDto
{
    [JsonPropertyName("businessKey")]
    public string? BusinessKey { get; set; }

    [JsonPropertyName("variables")]
    public IDictionary<string, CamundaVariableDto>? Variables { get; set; }
}
