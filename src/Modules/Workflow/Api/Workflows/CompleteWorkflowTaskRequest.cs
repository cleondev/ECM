using System.Collections.Generic;

namespace ECM.Workflow.Api.Workflows;

public sealed record CompleteWorkflowTaskRequest(
    string Action,
    string? Comment,
    IReadOnlyDictionary<string, object?>? Outputs);
