namespace ECM.Workflow.Application.Workflows.Commands;

public sealed record ReassignWorkflowTaskCommand(string TaskId, string AssigneeId);
