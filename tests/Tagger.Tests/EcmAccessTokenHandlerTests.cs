using System.Net;
using System.Net.Http;

using Ecm.Sdk.Authentication;
using Ecm.Sdk.Configuration;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Xunit;

namespace Tagger.Tests;

public class EcmAccessTokenHandlerTests
{
    [Fact]
    public async Task SendAsync_UsesManualUserContextWhenProvided()
    {
        ManualEcmUserContext.Clear();
        ManualEcmUserContext.SetUserKey("tagger@example.com");

        var apiResponses = new RecordingHttpMessageHandler
        {
            ResponseFactory = request =>
            {
                if (request.RequestUri!.AbsolutePath.EndsWith("/api/iam/auth/on-behalf", StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("{\"accessToken\":\"token\",\"expiresInMinutes\":5}")
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        };

        var integrationOptions = new EcmIntegrationOptions
        {
            BaseUrl = "https://example.test/",
            ApiKey = new ApiKeyOptions
            {
                ApiKey = "test",
                DefaultUserEmail = "fallback@example.com",
            }
        };

        var authenticator = new EcmAuthenticator(
            new HttpClient(apiResponses),
            Options.Create(integrationOptions),
            new MemoryCache(new MemoryCacheOptions()),
            NullLogger<EcmAuthenticator>.Instance);

        var innerHandler = new RecordingHttpMessageHandler
        {
            ResponseFactory = _ => new HttpResponseMessage(HttpStatusCode.OK),
        };

        var handler = new EcmAccessTokenHandler(
            authenticator,
            new HttpContextAccessor(),
            Options.Create(integrationOptions),
            NullLogger<EcmAccessTokenHandler>.Instance,
            new ServiceCollection()
                .AddSingleton<IEcmUserContext, ManualEcmUserContext>()
                .BuildServiceProvider())
        {
            InnerHandler = innerHandler
        };

        using var client = new HttpClient(handler);
        var response = await client.GetAsync("https://example.test/documents", CancellationToken.None);

        Assert.True(response.IsSuccessStatusCode);

        var onBehalfRequest = Assert.Single(apiResponses.Calls, call =>
            call.Request.RequestUri!.AbsolutePath.EndsWith("/api/iam/auth/on-behalf", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("tagger@example.com", onBehalfRequest.Body, StringComparison.OrdinalIgnoreCase);

        var outgoingRequest = Assert.Single(innerHandler.Calls);
        Assert.Equal("Bearer", outgoingRequest.Request.Headers.Authorization?.Scheme);
        Assert.Equal("token", outgoingRequest.Request.Headers.Authorization?.Parameter);
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        public required Func<HttpRequestMessage, HttpResponseMessage> ResponseFactory { get; init; }

        public List<(HttpRequestMessage Request, string? Body)> Calls { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Calls.Add((request, body));

            return ResponseFactory(request);
        }
    }
}
