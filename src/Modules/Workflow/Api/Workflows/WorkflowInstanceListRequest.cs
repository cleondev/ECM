using System;
using Microsoft.AspNetCore.Mvc;

namespace ECM.Workflow.Api.Workflows;

public sealed class WorkflowInstanceListRequest
{
    [FromQuery(Name = "state")]
    public string? State { get; init; }

    [FromQuery(Name = "document_id")]
    public Guid? DocumentId { get; init; }
}
