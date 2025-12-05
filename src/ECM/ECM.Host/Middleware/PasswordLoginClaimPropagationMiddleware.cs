using System;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Shared.Extensions.Http;

namespace ECM.Host.Middleware;

internal sealed class PasswordLoginClaimPropagationMiddleware
{
    private const string OnBehalfClaimType = "on_behalf";

    private readonly RequestDelegate _next;
    private readonly ILogger<PasswordLoginClaimPropagationMiddleware> _logger;

    public PasswordLoginClaimPropagationMiddleware(
        RequestDelegate next,
        ILogger<PasswordLoginClaimPropagationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var identity = ResolveTargetIdentity(context);
        if (identity is not null)
        {
            ApplyForwardedClaims(identity, context.Request.Headers);
        }

        await _next(context);
    }

    private ClaimsIdentity? ResolveTargetIdentity(HttpContext context)
    {
        if (!HasPasswordLoginForwardedHeaders(context.Request.Headers))
        {
            return null;
        }

        var forwardedIdentity = context.User.Identities
            .OfType<ClaimsIdentity>()
            .FirstOrDefault(identity =>
                string.Equals(
                    identity.AuthenticationType,
                    "PasswordLoginForwarding",
                    StringComparison.OrdinalIgnoreCase));

        if (forwardedIdentity is not null)
        {
            return forwardedIdentity;
        }

        var identity = new ClaimsIdentity(
            authenticationType: "PasswordLoginForwarding",
            nameType: ClaimTypes.Name,
            roleType: ClaimTypes.Role);

        if (context.User.Identity is null)
        {
            context.User = new ClaimsPrincipal(identity);
        }
        else
        {
            context.User.AddIdentity(identity);
        }

        _logger.LogDebug("Created password-login identity from forwarded headers for downstream request.");

        return identity;
    }

    private static bool HasPasswordLoginForwardedHeaders(IHeaderDictionary headers)
    {
        return headers.ContainsKey(PasswordLoginForwardingHeaders.UserId)
            || headers.ContainsKey(PasswordLoginForwardingHeaders.Email)
            || headers.ContainsKey(PasswordLoginForwardingHeaders.DisplayName)
            || headers.ContainsKey(PasswordLoginForwardingHeaders.PreferredUsername)
            || headers.ContainsKey(PasswordLoginForwardingHeaders.PrimaryGroupId)
            || headers.ContainsKey(PasswordLoginForwardingHeaders.PrimaryGroupName)
            || headers.ContainsKey(PasswordLoginForwardingHeaders.OnBehalf);
    }

    private void ApplyForwardedClaims(ClaimsIdentity identity, IHeaderDictionary headers)
    {
        var userIdHeader = GetHeaderValue(headers, PasswordLoginForwardingHeaders.UserId);
        if (!string.IsNullOrWhiteSpace(userIdHeader)
            && Guid.TryParse(userIdHeader, out var userId)
            && userId != Guid.Empty)
        {
            EnsureClaimValue(identity, ClaimTypes.NameIdentifier, userId.ToString());
        }

        var email = GetHeaderValue(headers, PasswordLoginForwardingHeaders.Email);
        if (!string.IsNullOrWhiteSpace(email))
        {
            AddClaimIfMissing(identity, ClaimTypes.Email, email);
            AddClaimIfMissing(identity, ClaimTypes.Upn, email);
            AddClaimIfMissing(identity, "preferred_username", email);
        }

        var displayName = GetHeaderValue(headers, PasswordLoginForwardingHeaders.DisplayName);
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            AddClaimIfMissing(identity, ClaimTypes.Name, displayName);
        }

        var primaryGroupIdHeader = GetHeaderValue(headers, PasswordLoginForwardingHeaders.PrimaryGroupId);
        if (!string.IsNullOrWhiteSpace(primaryGroupIdHeader)
            && Guid.TryParse(primaryGroupIdHeader, out var primaryGroupId)
            && primaryGroupId != Guid.Empty)
        {
            EnsureClaimValue(identity, "primary_group_id", primaryGroupId.ToString());
        }

        var primaryGroupName = GetHeaderValue(headers, PasswordLoginForwardingHeaders.PrimaryGroupName);
        if (!string.IsNullOrWhiteSpace(primaryGroupName))
        {
            EnsureClaimValue(identity, "primary_group_name", primaryGroupName);
        }

        var onBehalf = GetHeaderValue(headers, PasswordLoginForwardingHeaders.OnBehalf);
        if (!string.IsNullOrWhiteSpace(onBehalf))
        {
            AddClaimIfMissing(identity, OnBehalfClaimType, onBehalf);
        }
    }

    private static string? GetHeaderValue(IHeaderDictionary headers, string headerName)
    {
        if (!headers.TryGetValue(headerName, out var values))
        {
            return null;
        }

        var value = values.ToString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private void AddClaimIfMissing(ClaimsIdentity identity, string claimType, string value, bool matchValue = false)
    {
        if (identity.HasClaim(claim => claim.Type == claimType
            && (!matchValue
                || string.Equals(claim.Value, value, StringComparison.OrdinalIgnoreCase))))
        {
            return;
        }

        identity.AddClaim(new Claim(claimType, value));
        _logger.LogDebug("Propagated password-login claim {ClaimType} for downstream request.", claimType);
    }

    private void EnsureClaimValue(ClaimsIdentity identity, string claimType, string value)
    {
        foreach (var claim in identity.FindAll(claimType).ToArray())
        {
            if (string.Equals(claim.Value, value, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            identity.TryRemoveClaim(claim);
        }

        identity.AddClaim(new Claim(claimType, value));
        _logger.LogDebug(
            "Propagated password-login claim {ClaimType} with enforced value for downstream request.",
            claimType);
    }
}
