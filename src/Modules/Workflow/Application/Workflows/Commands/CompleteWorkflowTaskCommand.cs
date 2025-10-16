using System.Collections.Generic;

namespace ECM.Workflow.Application.Workflows.Commands;

public sealed record CompleteWorkflowTaskCommand(
    string TaskId,
    string Action,
    string? Comment,
    IReadOnlyDictionary<string, object?>? Outputs);
