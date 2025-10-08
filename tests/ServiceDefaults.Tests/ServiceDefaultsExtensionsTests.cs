using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using ServiceDefaults;
using Xunit;

namespace ServiceDefaults.Tests;

public class ServiceDefaultsExtensionsTests
{
    [Fact]
    public void AddServiceDefaults_RegistersHealthChecks()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddServiceDefaults();

        using var host = builder.Build();

        var healthService = host.Services.GetRequiredService<HealthCheckService>();

        Assert.NotNull(healthService);
    }

    [Fact]
    public void AddServiceDefaults_RegistersHttpClientFactory()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddServiceDefaults();

        using var host = builder.Build();

        var factory = host.Services.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient("resilient-test");

        Assert.NotNull(client);
    }
}
