using System;
using System.ComponentModel.DataAnnotations;

namespace ECM.Signature.Api.Requests;

public sealed class CreateSignatureRequestRequest
{
    [Required]
    public Guid DocumentId { get; init; }

    [Required]
    [EmailAddress]
    public string SignerEmail { get; init; } = string.Empty;
}
