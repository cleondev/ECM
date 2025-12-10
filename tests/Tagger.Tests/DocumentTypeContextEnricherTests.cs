using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;

using Ecm.Sdk.Clients;
using Ecm.Sdk.Configuration;
using Ecm.Sdk.Models.Documents;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Tagger.Events;
using Tagger.Rules.Configuration;
using Tagger.Rules.Enrichers;

using Xunit;

namespace Tagger.Tests;

public sealed class DocumentTypeContextEnricherTests
{
    [Fact]
    public void Enrich_AddsExtensionFromSdk_WhenTitleLacksExtension()
    {
        var documentId = Guid.NewGuid();
        var latestVersion = new DocumentVersionDto(
            Guid.NewGuid(),
            1,
            "uploads/sample.pdf",
            1,
            "application/pdf",
            "hash",
            Guid.NewGuid(),
            DateTimeOffset.UtcNow);

        var document = new DocumentDto(
            documentId,
            "DocumentTitle",
            "docType",
            "status",
            "sensitivity",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            Array.Empty<Guid>(),
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow,
            null,
            latestVersion,
            Array.Empty<DocumentTagDto>());

        var handler = new StubHttpMessageHandler(request =>
        {
            if (request.RequestUri?.AbsolutePath.EndsWith($"/api/documents/{documentId}", StringComparison.OrdinalIgnoreCase)
                == true)
            {
                var json = JsonSerializer.Serialize(document);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json"),
                };
            }

            return new HttpResponseMessage(HttpStatusCode.NotFound);
        });

        var httpClient = new HttpClient(handler);
        var options = Options.Create(new EcmIntegrationOptions { BaseUrl = "http://localhost" });
        var ecmClient = new EcmFileClient(httpClient, NullLogger<EcmFileClient>.Instance, options);
        var enricher = new DocumentTypeContextEnricher(ecmClient, NullLogger<DocumentTypeContextEnricher>.Instance);

        var integrationEvent = new DocumentUploadedIntegrationEvent(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow,
            documentId,
            "DocumentTitle",
            null,
            null,
            null,
            null);

        var builder = TaggingRuleContextBuilder.FromIntegrationEvent(integrationEvent);

        enricher.Enrich(builder, integrationEvent);

        var context = builder.Build();
        Assert.Equal(".pdf", context["extension"]);
    }
}

internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder ?? throw new ArgumentNullException(nameof(responder));
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_responder(request));
    }
}
