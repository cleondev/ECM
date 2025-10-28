using System;
using System.Net.Http.Headers;
using ECM.BuildingBlocks.Infrastructure.Configuration;
using ECM.Ocr.Application.Abstractions;
using ECM.Ocr.Infrastructure.DotOcr;
using ECM.Ocr.Infrastructure.Persistence;
using ECM.Ocr.Infrastructure.Services;
using EFCore.NamingConventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ECM.Ocr.Infrastructure;

public static class OcrInfrastructureModuleExtensions
{
    public static IServiceCollection AddOcrInfrastructure(this IServiceCollection services)
    {
        services.AddOptions<DotOcrOptions>()
            .BindConfiguration(DotOcrOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDbContext<OcrDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetRequiredConnectionStringForModule("Ocr");

            options.UseNpgsql(
                    connectionString,
                    builder => builder.MigrationsAssembly(typeof(OcrDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention();
        });

        services.AddHttpClient<DotOcrClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptionsMonitor<DotOcrOptions>>().CurrentValue;

            if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var baseUri))
            {
                throw new InvalidOperationException(
                    $"The configured Dot OCR base URL '{options.BaseUrl}' is not a valid absolute URI.");
            }

            client.BaseAddress = baseUri;

            if (options.TimeoutSeconds > 0)
            {
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            }

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
            }
        });

        services.AddScoped<IOcrProvider, DotOcrClient>();
        services.AddScoped<IDocumentFileLinkService, DocumentFileLinkService>();

        return services;
    }
}
