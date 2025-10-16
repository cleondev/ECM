using System;
using Microsoft.AspNetCore.Mvc;

namespace ECM.Workflow.Api.Workflows;

public sealed class WorkflowTaskListRequest
{
    [FromQuery(Name = "assignee_id")]
    public string? AssigneeId { get; init; }

    [FromQuery(Name = "state")]
    public string? State { get; init; }

    [FromQuery(Name = "document_id")]
    public Guid? DocumentId { get; init; }
}
