using System;
using System.ComponentModel.DataAnnotations;

namespace ECM.Modules.Signature.Api.Requests;

public sealed class CreateSignatureRequest
{
    [Required]
    public Guid DocumentId { get; init; }

    [Required]
    [EmailAddress]
    public string SignerEmail { get; init; } = string.Empty;
}
