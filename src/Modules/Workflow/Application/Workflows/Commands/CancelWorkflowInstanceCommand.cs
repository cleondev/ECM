namespace ECM.Workflow.Application.Workflows.Commands;

public sealed record CancelWorkflowInstanceCommand(string InstanceId, string? Reason);
