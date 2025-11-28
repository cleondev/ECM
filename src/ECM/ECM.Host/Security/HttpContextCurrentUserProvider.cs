using System;
using System.Security.Claims;
using ECM.Abstractions.Security;
using Microsoft.AspNetCore.Http;

namespace ECM.Host.Security;

public sealed class HttpContextCurrentUserProvider(IHttpContextAccessor httpContextAccessor) : ICurrentUserProvider
{
    private static readonly string[] CandidateUserIdClaims = ["oid", ClaimTypes.NameIdentifier];
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public ClaimsPrincipal? Current => _httpContextAccessor.HttpContext?.User;

    public Guid? TryGetUserId()
    {
        var principal = Current;
        if (principal is null)
        {
            return null;
        }

        foreach (var claimType in CandidateUserIdClaims)
        {
            var claimValue = principal.FindFirstValue(claimType);
            if (Guid.TryParse(claimValue, out var parsed) && parsed != Guid.Empty)
            {
                return parsed;
            }
        }

        return null;
    }

    public string? TryGetDisplayName()
    {
        var principal = Current;
        if (principal is null)
        {
            return null;
        }

        var displayName = principal.Identity?.Name ?? principal.FindFirst("name")?.Value;
        return string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
    }
}
