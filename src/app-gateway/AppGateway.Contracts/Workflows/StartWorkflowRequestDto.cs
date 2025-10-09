using System.ComponentModel.DataAnnotations;

namespace AppGateway.Contracts.Workflows;

public sealed class StartWorkflowRequestDto
{
    [Required]
    public Guid DocumentId { get; init; }

    [Required]
    [StringLength(128)]
    public string Definition { get; init; } = string.Empty;
}
