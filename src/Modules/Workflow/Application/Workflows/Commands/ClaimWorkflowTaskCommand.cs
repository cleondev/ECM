namespace ECM.Workflow.Application.Workflows.Commands;

public sealed record ClaimWorkflowTaskCommand(string TaskId, string AssigneeId);
