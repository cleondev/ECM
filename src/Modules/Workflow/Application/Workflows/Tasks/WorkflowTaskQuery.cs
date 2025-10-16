using System;

namespace ECM.Workflow.Application.Workflows.Tasks;

public sealed record WorkflowTaskQuery(string? AssigneeId, string? State, Guid? DocumentId);
