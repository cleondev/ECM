using System.ComponentModel.DataAnnotations;

namespace ECM.Workflow.Infrastructure.Options;

public sealed class CamundaOptions
{
    public const string SectionName = "Workflow:Camunda";

    [Required]
    [Url]
    public string BaseUrl { get; set; } = "http://localhost:8080/engine-rest";

    public string? TenantId { get; set; }
}
