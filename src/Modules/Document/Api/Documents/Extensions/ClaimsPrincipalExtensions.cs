using System;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using ECM.Abstractions.Users;

namespace ECM.Document.Api.Documents.Extensions;

internal static class ClaimsPrincipalExtensions
{
    private static readonly string[] CandidateUpnClaimTypes =
    [
        ClaimTypes.Upn,
        "preferred_username",
        ClaimTypes.Email
    ];

    public static async Task<Guid?> GetUserObjectIdAsync(
        this ClaimsPrincipal? principal,
        IUserLookupService userLookupService,
        CancellationToken cancellationToken = default)
    {
        if (principal is null)
        {
            return null;
        }

        ArgumentNullException.ThrowIfNull(userLookupService);

        foreach (var claimType in CandidateUpnClaimTypes)
        {
            var claim = principal.FindFirst(claimType);
            if (claim is null || string.IsNullOrWhiteSpace(claim.Value))
            {
                continue;
            }

            var userId = await userLookupService.FindUserIdByUpnAsync(claim.Value, cancellationToken);
            if (userId.HasValue)
            {
                return userId.Value;
            }
        }

        return null;
    }
}
