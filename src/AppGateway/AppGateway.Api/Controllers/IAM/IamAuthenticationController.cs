using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using AppGateway.Api.Auth;
using AppGateway.Contracts.IAM.Groups;
using AppGateway.Contracts.IAM.Roles;
using AppGateway.Contracts.IAM.Users;
using AppGateway.Infrastructure.Ecm;
using AppGateway.Infrastructure.Auth;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppGateway.Api.Controllers.IAM;

[ApiController]
[Route("api/iam")]
public sealed class IamAuthenticationController(
    IUsersApiClient client,
    IUserProvisioningService provisioningService,
    IOptionsSnapshot<CookieAuthenticationOptions> cookieOptions,
    ILogger<IamAuthenticationController> logger) : ControllerBase
{
    private readonly IUsersApiClient _client = client;
    private readonly IUserProvisioningService _provisioningService = provisioningService;
    private readonly CookieAuthenticationOptions _cookieOptions = cookieOptions
        .Get(CookieAuthenticationDefaults.AuthenticationScheme);
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
        var silentLoginUrl = AzureLoginRedirectHelper.CreateLoginUrl(
            HttpContext,
            resolvedRedirect,
            AzureLoginRedirectHelper.AzureLoginMode.Silent);

        if (User.Identity?.IsAuthenticated != true)
        {
            return Ok(new CheckLoginResponseDto(false, resolvedRedirect, loginUrl, silentLoginUrl, null));
        }

        var profileResolution = await CurrentUserProfileResolver.ResolveAsync(
            HttpContext,
            _client,
            _logger,
            cancellationToken,
            fetchFromApiWhenMissing: false);

        if (profileResolution.HasProfile)
        {
            return Ok(new CheckLoginResponseDto(true, resolvedRedirect, null, null, profileResolution.Profile));
        }

        if (profileResolution.RequiresSignOut)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new CheckLoginResponseDto(false, resolvedRedirect, loginUrl, silentLoginUrl, null));
        }

        try
        {
            var provisionedProfile = await _provisioningService.EnsureUserExistsAsync(User, cancellationToken);
            var profile = provisionedProfile ?? await _client.GetCurrentUserProfileAsync(cancellationToken);

            if (profile is null)
            {
                _logger.LogWarning(
                    "Authenticated principal did not resolve to a user profile. Treating as unauthenticated.");
                return Ok(new CheckLoginResponseDto(false, resolvedRedirect, loginUrl, silentLoginUrl, null));
            }

            return Ok(new CheckLoginResponseDto(true, resolvedRedirect, null, null, profile));
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "An error occurred while checking the current user's login state.");
            return Ok(new CheckLoginResponseDto(false, resolvedRedirect, loginUrl, silentLoginUrl, null));
        }
    }

    [HttpPost("auth/on-behalf")]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.AuthenticationScheme)]
    [ProducesResponseType(typeof(OnBehalfLoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SignInOnBehalfAsync(
        [FromBody] OnBehalfLoginRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserEmail) && request.UserId is null)
        {
            return BadRequest(new { message = "Either user email or user id must be provided." });
        }

        UserSummaryDto? profile = null;

        if (request.UserId is { } userId && userId != Guid.Empty)
        {
            profile = await _client.GetUserAsync(userId, cancellationToken);
        }

        if (profile is null && !string.IsNullOrWhiteSpace(request.UserEmail))
        {
            profile = await _client.GetUserByEmailAsync(request.UserEmail.Trim(), cancellationToken);
        }

        if (profile is null)
        {
            return NotFound(new { message = "User not found." });
        }

        var caller = HttpContext.User?.Identity?.Name;
        var principal = CreateLocalUserPrincipal(
            profile,
            authenticationMethod: "on-behalf",
            isOnBehalf: true,
            onBehalfBy: caller);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        var expiresOn = DateTimeOffset.UtcNow + _cookieOptions.ExpireTimeSpan;

        return Ok(new OnBehalfLoginResponseDto(profile, expiresOn));
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

        var principal = CreateLocalUserPrincipal(profile, authenticationMethod: "password");

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        _logger.LogInformation("Password login succeeded for {Email}.", profile.Email);

        return Ok(new CheckLoginResponseDto(true, resolvedRedirect, null, null, profile));
    }

    private static ClaimsPrincipal CreateLocalUserPrincipal(
        UserSummaryDto user,
        string authenticationMethod,
        bool isOnBehalf = false,
        string? onBehalfBy = null)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
        identity.AddClaim(new Claim(ClaimTypes.Name, user.DisplayName));
        identity.AddClaim(new Claim(ClaimTypes.Email, user.Email));
        identity.AddClaim(new Claim(ClaimTypes.Upn, user.Email));
        identity.AddClaim(new Claim("preferred_username", user.Email));
        identity.AddClaim(new Claim("auth_method", authenticationMethod));
        identity.AddClaim(new Claim(PasswordLoginClaims.MarkerClaimType, PasswordLoginClaims.MarkerClaimValue));
        identity.AddClaim(new Claim(PasswordLoginClaims.ProfileClaimType, JsonSerializer.Serialize(user)));

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

        foreach (var groupId in user.GroupIds?.Where(id => id != Guid.Empty).Distinct() ?? [])
        {
            identity.AddClaim(new Claim("group_id", groupId.ToString()));
        }

        foreach (var role in user.Roles ?? [])
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role.Name));
        }

        foreach (var group in user.Groups ?? [])
        {
            identity.AddClaim(new Claim("group", group.Name));
        }

        if (isOnBehalf)
        {
            identity.AddClaim(new Claim(PasswordLoginClaims.OnBehalfClaimType, "true"));

            if (!string.IsNullOrWhiteSpace(onBehalfBy))
            {
                identity.AddClaim(new Claim("on_behalf_by", onBehalfBy.Trim()));
            }
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

public sealed class OnBehalfLoginRequest
{
    public string? UserEmail { get; init; }

    public Guid? UserId { get; init; }
}

public sealed record OnBehalfLoginResponseDto(UserSummaryDto User, DateTimeOffset ExpiresOn);
