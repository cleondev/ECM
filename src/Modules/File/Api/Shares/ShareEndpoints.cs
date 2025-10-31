using System.Net;
using System.Security.Claims;
using System.Linq;
using ECM.File.Application.Shares;
using ECM.File.Domain.Shares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

namespace ECM.File.Api.Shares;

public static class ShareEndpoints
{
    public static RouteGroupBuilder MapShareEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/ecm/shares");
        group.WithTags("Share Links");
        group.WithGroupName(FileSwagger.DocumentName);

        group.MapPost("/", CreateShareAsync)
            .WithName("CreateShareLink")
            .WithDescription("Creates a new short share link for a document or version.");

        group.MapGet("/{shareId:guid}", GetShareAsync)
            .WithName("GetShareLink");

        group.MapPatch("/{shareId:guid}", UpdateShareAsync)
            .WithName("UpdateShareLink");

        group.MapDelete("/{shareId:guid}", RevokeShareAsync)
            .WithName("DeleteShareLink");

        group.MapGet("/{shareId:guid}/stats", GetShareStatsAsync)
            .WithName("GetShareLinkStats");

        group.MapPost("/{shareId:guid}/revoke", PostRevokeShareAsync)
            .WithName("RevokeShareLink");

        return group;
    }

    public static void MapPublicShareEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapGet("/s/{code}", GetInterstitialAsync)
            .WithName("GetShareInterstitial");

        builder.MapPost("/s/{code}/password", VerifyPasswordAsync)
            .WithName("VerifySharePassword");

        builder.MapPost("/s/{code}/presign", CreatePresignedUrlAsync)
            .WithName("CreateSharePresignedUrl");

        builder.MapGet("/s/{code}/download", RedirectToDownloadAsync)
            .WithName("DownloadSharedFile");
    }

    private static async Task<Results<Created<ShareLinkDto>, ValidationProblem>> CreateShareAsync(
        [FromBody] CreateShareLinkRequest request,
        CreateShareLinkCommandHandler handler,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var ownerId = GetUserId(httpContext.User);
        if (ownerId is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["owner"] = ["Authenticated user context is required to create share links."]
            });
        }

        var requestBaseUrl = ResolveRequestBaseUrl(httpContext.Request);
        var command = request.ToCommand(ownerId.Value, requestBaseUrl);
        var result = await handler.HandleAsync(command, cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["share"] = [.. result.Errors]
            });
        }

        return TypedResults.Created($"/api/ecm/shares/{result.Value.Id}", result.Value);
    }

    private static async Task<IResult> GetShareAsync(Guid shareId, ShareLinkService service, CancellationToken cancellationToken)
    {
        var share = await service.GetByIdAsync(shareId, cancellationToken);
        return share is null ? TypedResults.NotFound() : TypedResults.Ok(share);
    }

    private static async Task<IResult> UpdateShareAsync(
        Guid shareId,
        [FromBody] UpdateShareLinkRequest request,
        UpdateShareLinkCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(shareId);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsFailure ? MapErrors(result.Errors) : TypedResults.NoContent();
    }

    private static async Task<IResult> RevokeShareAsync(
        Guid shareId,
        ShareLinkService service,
        CancellationToken cancellationToken)
    {
        var result = await service.RevokeAsync(shareId, cancellationToken);
        return result.IsFailure ? MapErrors(result.Errors) : TypedResults.NoContent();
    }

    private static async Task<IResult> GetShareStatsAsync(
        Guid shareId,
        ShareLinkService service,
        CancellationToken cancellationToken)
    {
        var stats = await service.GetStatisticsAsync(shareId, cancellationToken);
        return stats is null ? TypedResults.NotFound() : TypedResults.Ok(stats);
    }

    private static async Task<IResult> PostRevokeShareAsync(
        Guid shareId,
        ShareLinkService service,
        CancellationToken cancellationToken)
    {
        var result = await service.RevokeAsync(shareId, cancellationToken);
        return result.IsFailure ? MapErrors(result.Errors) : TypedResults.NoContent();
    }

    private static string? ResolveRequestBaseUrl(HttpRequest request)
    {
        if (!request.Host.HasValue || string.IsNullOrWhiteSpace(request.Scheme))
        {
            return null;
        }

        var host = request.Host.ToUriComponent();
        var pathBase = request.PathBase.HasValue ? request.PathBase.ToUriComponent() : string.Empty;

        return string.Concat(request.Scheme, "://", host, pathBase);
    }

    private static async Task<IResult> GetInterstitialAsync(
        string code,
        [FromQuery(Name = "password")] string? password,
        ShareAccessService accessService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var remoteIp = httpContext.Connection.RemoteIpAddress;
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        var result = await accessService.GetInterstitialAsync(
            code,
            httpContext.User,
            password,
            remoteIp,
            userAgent,
            cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            return MapErrors(result.Errors);
        }

        return TypedResults.Ok(result.Value);
    }

    private static async Task<IResult> VerifyPasswordAsync(
        string code,
        [FromBody] VerifySharePasswordRequest request,
        ShareAccessService accessService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var remoteIp = httpContext.Connection.RemoteIpAddress;
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        var result = await accessService.VerifyPasswordAsync(
            code,
            request.Password,
            httpContext.User,
            remoteIp,
            userAgent,
            cancellationToken);

        return result.IsFailure
            ? MapErrors(result.Errors)
            : TypedResults.Ok(new { success = true });
    }

    private static async Task<IResult> CreatePresignedUrlAsync(
        string code,
        [FromBody] SharePresignRequest request,
        ShareAccessService accessService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var remoteIp = httpContext.Connection.RemoteIpAddress;
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        var result = await accessService.CreateDownloadLinkAsync(
            code,
            httpContext.User,
            request.Password,
            remoteIp,
            userAgent,
            cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            return MapErrors(result.Errors);
        }

        return TypedResults.Ok(new
        {
            url = result.Value.Uri.ToString(),
            expiresAtUtc = result.Value.ExpiresAtUtc,
        });
    }

    private static async Task<IResult> RedirectToDownloadAsync(
        string code,
        [FromQuery(Name = "password")] string? password,
        ShareAccessService accessService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var remoteIp = httpContext.Connection.RemoteIpAddress;
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();

        var result = await accessService.CreateDownloadLinkAsync(
            code,
            httpContext.User,
            password,
            remoteIp,
            userAgent,
            cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            return MapErrors(result.Errors);
        }

        return TypedResults.Redirect(result.Value.Uri.ToString(), permanent: false);
    }

    private static readonly string[] CandidateOwnerClaimTypes =
    [
        ClaimTypes.NameIdentifier,
        "http://schemas.microsoft.com/identity/claims/objectidentifier",
        "oid",
        "sub",
    ];

    private static Guid? GetUserId(ClaimsPrincipal? user)
    {
        if (user?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        foreach (var claimType in CandidateOwnerClaimTypes)
        {
            var identifier = user.FindFirstValue(claimType);
            if (string.IsNullOrWhiteSpace(identifier))
            {
                continue;
            }

            if (Guid.TryParse(identifier, out var id))
            {
                return id;
            }
        }

        return null;
    }

    private static IResult MapErrors(IReadOnlyCollection<string> errors)
    {
        if (errors.Count == 0)
        {
            return TypedResults.BadRequest();
        }

        if (errors.Contains("ShareNotFound", StringComparer.OrdinalIgnoreCase))
        {
            return TypedResults.NotFound();
        }

        if (errors.Contains("ShareNotAuthorized", StringComparer.OrdinalIgnoreCase))
        {
            return TypedResults.Forbid();
        }

        if (errors.Contains("PasswordInvalid", StringComparer.OrdinalIgnoreCase)
            || errors.Contains("PasswordRequired", StringComparer.OrdinalIgnoreCase))
        {
            return TypedResults.BadRequest(new { errors });
        }

        if (errors.Contains("ShareViewQuotaExceeded", StringComparer.OrdinalIgnoreCase)
            || errors.Contains("ShareDownloadQuotaExceeded", StringComparer.OrdinalIgnoreCase))
        {
            return TypedResults.StatusCode(StatusCodes.Status403Forbidden);
        }

        return TypedResults.BadRequest(new { errors });
    }
}
