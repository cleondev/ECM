using System;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Api.Auth;
using AppGateway.Contracts.IAM.Groups;
using AppGateway.Contracts.IAM.Roles;
using AppGateway.Contracts.IAM.Users;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace AppGateway.Api.Controllers.IAM;

[ApiController]
[Route("api/iam")]
public sealed class IamAuthenticationController(
    IEcmApiClient client,
    IUserProvisioningService provisioningService,
    ILogger<IamAuthenticationController> logger) : ControllerBase
{
    private readonly IEcmApiClient _client = client;
    private readonly IUserProvisioningService _provisioningService = provisioningService;
    private readonly ILogger<IamAuthenticationController> _logger = logger;

    [HttpGet("check-login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CheckLoginResponseDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckLoginAsync(
        [FromQuery] string? redirectUri,
        CancellationToken cancellationToken)
    {
        var resolvedRedirect = AzureLoginRedirectHelper.ResolveRedirectPath(
            HttpContext,
            redirectUri,
            Program.MainAppPath);

        var loginUrl = AzureLoginRedirectHelper.CreateLoginUrl(HttpContext, resolvedRedirect);

        if (User.Identity?.IsAuthenticated != true)
        {
            return Ok(new CheckLoginResponseDto(false, resolvedRedirect, loginUrl, null));
        }

        try
        {
            var provisionedProfile = await _provisioningService.EnsureUserExistsAsync(User, cancellationToken);
            var profile = provisionedProfile ?? await _client.GetCurrentUserProfileAsync(cancellationToken);

            if (profile is null)
            {
                _logger.LogWarning(
                    "Authenticated principal did not resolve to a user profile. Treating as unauthenticated.");
                return Ok(new CheckLoginResponseDto(false, resolvedRedirect, loginUrl, null));
            }

            return Ok(new CheckLoginResponseDto(true, resolvedRedirect, null, profile));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "An error occurred while checking the current user's login state.");
            return Ok(new CheckLoginResponseDto(false, resolvedRedirect, loginUrl, null));
        }
    }

    [HttpPost("password-login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(CheckLoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> PasswordLoginAsync([
        FromBody] PasswordLoginRequest request,
        CancellationToken cancellationToken)
    {
        var resolvedRedirect = AzureLoginRedirectHelper.ResolveRedirectPath(
            HttpContext,
            request.RedirectUri,
            Program.MainAppPath);

        UserSummaryDto? profile;
        try
        {
            var email = request.Email?.Trim() ?? string.Empty;
            profile = await _client.AuthenticateUserAsync(
                new AuthenticateUserRequestDto
                {
                    Email = email,
                    Password = request.Password,
                },
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(
                ex,
                "Password login failed due to upstream error for {Email}.",
                request.Email);
            return StatusCode(StatusCodes.Status502BadGateway, new { message = "Login service is unavailable." });
        }

        if (profile is null)
        {
            _logger.LogWarning(
                "Password login rejected due to invalid credentials for {Email}.",
                request.Email?.Trim());
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var principal = CreateLocalUserPrincipal(profile);

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        _logger.LogInformation("Password login succeeded for {Email}.", profile.Email);

        return Ok(new CheckLoginResponseDto(true, resolvedRedirect, null, profile));
    }

    private static ClaimsPrincipal CreateLocalUserPrincipal(UserSummaryDto user)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.DisplayName));
        identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
        identity.AddClaim(new Claim("preferred_username", user.Email));

        if (user.PrimaryGroupId.HasValue && user.PrimaryGroupId.Value != Guid.Empty)
        {
            identity.AddClaim(new Claim("primary_group_id", user.PrimaryGroupId.Value.ToString()));

            var primaryGroupName = user.Groups?
                .FirstOrDefault(group => group.Id == user.PrimaryGroupId.Value)?
                .Name;

            if (!string.IsNullOrWhiteSpace(primaryGroupName))
            {
                identity.AddClaim(new Claim("primary_group_name", primaryGroupName));
            }
        }

        foreach (var groupId in user.GroupIds?.Where(id => id != Guid.Empty).Distinct() ?? Array.Empty<Guid>())
        {
            identity.AddClaim(new Claim("group_id", groupId.ToString()));
        }

        foreach (var role in user.Roles ?? Array.Empty<RoleSummaryDto>())
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role.Name));
        }

        foreach (var group in user.Groups ?? Array.Empty<GroupSummaryDto>())
        {
            identity.AddClaim(new Claim("group", group.Name));
        }

        return new ClaimsPrincipal(identity);
    }
}

public sealed class PasswordLoginRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string? RedirectUri { get; init; }
}
