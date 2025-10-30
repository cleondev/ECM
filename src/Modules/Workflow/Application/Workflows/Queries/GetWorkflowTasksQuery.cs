using System;

namespace ECM.Workflow.Application.Workflows.Queries;

public sealed record GetWorkflowTasksQuery(string? AssigneeId, string? State, Guid? DocumentId);
