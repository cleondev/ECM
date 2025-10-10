using System;
using System.ComponentModel.DataAnnotations;

namespace ECM.Workflow.Api.Workflows;

public sealed class StartWorkflowRequest
{
    [Required]
    public Guid DocumentId { get; init; }

    [Required]
    [StringLength(128)]
    public string Definition { get; init; } = string.Empty;
}
