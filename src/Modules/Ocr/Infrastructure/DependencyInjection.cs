using ECM.BuildingBlocks.Infrastructure.Configuration;
using ECM.Ocr.Application.Abstractions;
using ECM.Ocr.Infrastructure.Persistence;
using EFCore.NamingConventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.Ocr.Infrastructure;

public static class OcrInfrastructureModuleExtensions
{
    public static IServiceCollection AddOcrInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<OcrDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetRequiredConnectionStringForModule("Ocr");

            options.UseNpgsql(
                    connectionString,
                    builder => builder.MigrationsAssembly(typeof(OcrDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IOcrProvider, DatabaseOcrProvider>();

        return services;
    }
}
