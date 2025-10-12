using System;
namespace ECM.Workflow.Application.Workflows.Commands;

public sealed record StartWorkflowCommand(Guid DocumentId, string Definition);
