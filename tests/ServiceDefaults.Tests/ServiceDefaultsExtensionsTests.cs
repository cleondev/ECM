using System.Net.Http;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Infrastructure.Caching;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Logs;
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

    [Fact]
    public async Task AddServiceDefaults_UsesNoCacheProviderByDefault()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddServiceDefaults();

        using var host = builder.Build();

        var cache = host.Services.GetRequiredService<ICacheProvider>();

        var lookup = await cache.GetAsync<string>("missing");
        Assert.False(lookup.Found);

        var invocationCount = 0;
        var first = await cache.GetOrCreateAsync("no-cache-key", _ =>
        {
            invocationCount++;
            return Task.FromResult("value-1");
        });

        var second = await cache.GetOrCreateAsync("no-cache-key", _ =>
        {
            invocationCount++;
            return Task.FromResult("value-2");
        });

        Assert.Equal(2, invocationCount);
        Assert.Equal("value-2", second);
        Assert.Equal("value-1", first);
    }

    [Fact]
    public async Task AddServiceDefaults_RegistersMemoryCacheProvider()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration["Cache:Mode"] = CacheMode.Memory.ToString();
        builder.AddServiceDefaults();

        using var host = builder.Build();

        var cache = host.Services.GetRequiredService<ICacheProvider>();

        var invocationCount = 0;
        var first = await cache.GetOrCreateAsync("memory-cache-key", _ =>
        {
            invocationCount++;
            return Task.FromResult("cached-value");
        });

        var second = await cache.GetOrCreateAsync("memory-cache-key", _ =>
        {
            invocationCount++;
            return Task.FromResult("new-value");
        });

        Assert.Equal(1, invocationCount);
        Assert.Equal("cached-value", first);
        Assert.Equal("cached-value", second);
    }

    [Fact]
    public void AddServiceDefaults_RegistersOpenTelemetryLogging()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.AddServiceDefaults();

        using var host = builder.Build();

        var providers = host.Services.GetServices<ILoggerProvider>();

        Assert.Contains(providers, provider => provider is OpenTelemetryLoggerProvider);
    }
}
