using Microsoft.AspNetCore.Mvc;

namespace ECM.Signature.Api.Requests;

public sealed class SignatureListRequest
{
    [FromQuery(Name = "status")]
    public string? Status { get; init; }
}
