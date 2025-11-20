using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using samples.EcmFileIntegrationSample;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<EcmIntegrationOptions>(builder.Configuration.GetSection("Ecm"));
builder.Services.AddHttpClient<EcmFileClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<EcmIntegrationOptions>>().Value;

    if (string.IsNullOrWhiteSpace(options.BaseUrl))
    {
        throw new InvalidOperationException("Ecm:BaseUrl must be configured in appsettings.json or environment variables.");
    }

    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(100);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("ecm-integration-sample/1.0");

    if (!string.IsNullOrWhiteSpace(options.AccessToken))
    {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            options.AccessToken);
    }
});

builder.Services.AddHostedService<FileUploadDemo>();

var host = builder.Build();
await host.RunAsync();
