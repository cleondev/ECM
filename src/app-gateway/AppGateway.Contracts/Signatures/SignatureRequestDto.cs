using System.ComponentModel.DataAnnotations;

namespace AppGateway.Contracts.Signatures;

public sealed class SignatureRequestDto
{
    [Required]
    public Guid DocumentId { get; init; }

    [Required]
    [EmailAddress]
    public string SignerEmail { get; init; } = string.Empty;
}
