using System.ComponentModel.DataAnnotations;

namespace AppGateway.Contracts.Documents;

public sealed class CreateDocumentRequestDto
{
    [Required]
    [StringLength(256)]
    public string Title { get; init; } = string.Empty;
}
