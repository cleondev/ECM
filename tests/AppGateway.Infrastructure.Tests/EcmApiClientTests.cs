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
    public async Task ForwardApiKeyAuthorizationHeaderWhenNoBearerToken()
    {
        const string apiKey = "ApiKey test-key";
        var capturedAuthorization = default(string?);

        var handler = new RecordingHttpHandler(message =>
        {
            capturedAuthorization = message.Headers.Authorization?.ToString();
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
        httpContext.Request.Headers.Authorization = apiKey;

        var client = new EcmApiClient(
            httpClient,
            new HttpContextAccessor { HttpContext = httpContext },
            new NoopTokenAcquisition(),
            new OptionsWrapper<EcmApiClientOptions>(new EcmApiClientOptions { Scope = string.Empty }),
            NullLogger<EcmApiClient>.Instance);

        var profile = await client.GetUserByEmailAsync("user@example.com");

        Assert.NotNull(profile);
        Assert.Equal(apiKey, capturedAuthorization);
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
            string? scope = null,
            string? authenticationScheme = null,
            string? tenant = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
            => throw new NotImplementedException();

        public Task<string> GetAccessTokenForUserAsync(
            IEnumerable<string> scopes,
            string? authenticationScheme = null,
            string? tenantId = null,
            ClaimsPrincipal? user = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
            => throw new NotImplementedException();

        public Task<string> GetAccessTokenOnBehalfOfAsync(
            IEnumerable<string> scopes,
            string? authenticationScheme = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
            => throw new NotImplementedException();

        public Task<AuthenticationResult> GetAuthenticationResultForUserAsync(
            IEnumerable<string> scopes,
            string? authenticationScheme = null,
            string? tenantId = null,
            ClaimsPrincipal? user = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
            => throw new NotImplementedException();

        public Task<AuthenticationResult> GetAuthenticationResultOnBehalfOfAsync(
            IEnumerable<string> scopes,
            string? authenticationScheme = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
            => throw new NotImplementedException();

        public Task RemoveAccountAsync(ClaimsPrincipal user, string? authenticationScheme = null)
            => Task.CompletedTask;

        public Task ReplyForbiddenWithWwwAuthenticateHeaderAsync(
            IEnumerable<string> scopes,
            MsalUiRequiredException msalServiceException,
            string? authenticationScheme = null,
            string? userflow = null,
            HttpResponse? httpResponse = null)
            => Task.CompletedTask;

        public Task AddAccountToCacheFromAuthorizationCodeAsync(
            IEnumerable<string> scopes,
            string authorizationCode,
            string? redirectUri = null,
            string? authenticationScheme = null,
            ClaimsPrincipal? user = null,
            TokenAcquisitionOptions? tokenAcquisitionOptions = null)
            => Task.CompletedTask;

        public Task<IEnumerable<string>> GetAccountIdentifiersAsync(string? authenticationScheme = null)
            => Task.FromResult<IEnumerable<string>>([]);
    }
}
