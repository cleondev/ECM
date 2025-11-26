using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using AppGateway.Contracts.IAM.Users;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;
using Xunit;

namespace AppGateway.Infrastructure.Tests;

public sealed class EcmApiClientTests
{
    [Fact]
    public async Task ForwardApiKeyHeaderWhenNoBearerToken()
    {
        const string apiKey = "test-key";
        var capturedApiKey = default(string?);

        var handler = new RecordingHttpHandler(message =>
        {
            capturedApiKey = message.Headers.TryGetValues("X-Api-Key", out var values)
                ? values.FirstOrDefault()
                : null;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(new UserSummaryDto(
                    Guid.NewGuid(),
                    "user@example.com",
                    "User",
                    true,
                    false,
                    DateTimeOffset.UtcNow,
                    null,
                    [],
                    [],
                    []))
            };
        });

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://example.com")
        };

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Append("X-Api-Key", apiKey);

        var client = new EcmApiClient(
            httpClient,
            new HttpContextAccessor { HttpContext = httpContext },
            new NoopTokenAcquisition(),
            new OptionsWrapper<EcmApiClientOptions>(new EcmApiClientOptions { Scope = string.Empty }),
            NullLogger<EcmApiClient>.Instance);

        var profile = await client.GetUserByEmailAsync("user@example.com");

        Assert.NotNull(profile);
        Assert.Equal(apiKey, capturedApiKey);
    }

    private sealed class RecordingHttpHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(responder(request));
    }

    private sealed class NoopTokenAcquisition : ITokenAcquisition
    {
        public Task<string> GetAccessTokenForAppAsync(
            string scope,
            string? authenticationScheme,
            string? tenant = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
            => throw new NotImplementedException();

        public Task<string> GetAccessTokenForUserAsync(
            IEnumerable<string> scopes,
            string? authenticationScheme,
            string? tenantId = null,
            string? userFlow = null,
            ClaimsPrincipal? user = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
            => throw new NotImplementedException();

        public Task<AuthenticationResult> GetAuthenticationResultForUserAsync(
            IEnumerable<string> scopes,
            string? authenticationScheme,
            string? tenantId = null,
            string? userFlow = null,
            ClaimsPrincipal? user = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
            => throw new NotImplementedException();

        public Task<AuthenticationResult> GetAuthenticationResultForAppAsync(
            string scope,
            string? authenticationScheme,
            string? tenant = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
            => throw new NotImplementedException();

        public void ReplyForbiddenWithWwwAuthenticateHeader(
            IEnumerable<string> scopes,
            MsalUiRequiredException msalServiceException,
            string? authenticationScheme,
            HttpResponse? httpResponse = null)
        {
        }

        public string GetEffectiveAuthenticationScheme(string? authenticationScheme)
            => authenticationScheme ?? string.Empty;

        public Task ReplyForbiddenWithWwwAuthenticateHeaderAsync(
            IEnumerable<string> scopes,
            MsalUiRequiredException msalServiceException,
            HttpResponse? httpResponse = null)
            => Task.CompletedTask;
    }
}
