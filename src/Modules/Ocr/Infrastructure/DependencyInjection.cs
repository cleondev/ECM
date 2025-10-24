using ECM.Ocr.Application.Abstractions;
using ECM.Ocr.Infrastructure.DotOcr;
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
            .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _), "Invalid Dot OCR base URL.")
            .ValidateOnStart();

        services.AddHttpClient<DotOcrClient>((provider, client) =>
        {
            var options = provider.GetRequiredService<IOptionsMonitor<DotOcrOptions>>().CurrentValue;
            client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromSeconds(Math.Max(1, options.TimeoutSeconds));

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                client.DefaultRequestHeaders.Remove("X-Api-Key");
                client.DefaultRequestHeaders.Add("X-Api-Key", options.ApiKey);
            }
        });

        services.AddScoped<IOcrProvider>(static provider => provider.GetRequiredService<DotOcrClient>());

        return services;
    }
}
