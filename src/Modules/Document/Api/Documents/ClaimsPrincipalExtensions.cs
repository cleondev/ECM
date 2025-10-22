using System;
using System.Security.Claims;

namespace ECM.Document.Api.Documents;

internal static class ClaimsPrincipalExtensions
{
    private static readonly string[] CandidateClaimTypes =
    [
        "oid",
        ClaimTypes.NameIdentifier,
        ClaimTypes.Upn,
        "sub"
    ];

    public static Guid? GetUserObjectId(this ClaimsPrincipal? principal)
    {
        if (principal is null)
        {
            return null;
        }

        foreach (var claimType in CandidateClaimTypes)
        {
            var claim = principal.FindFirst(claimType);
            if (claim is null)
            {
                continue;
            }

            if (Guid.TryParse(claim.Value, out var userId))
            {
                return userId;
            }
        }

        return null;
    }
}
