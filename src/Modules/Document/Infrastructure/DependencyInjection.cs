using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.BuildingBlocks.Infrastructure.Time;
using ECM.Modules.Document.Domain.Documents;
using ECM.Modules.Document.Infrastructure.Documents;
using ECM.Modules.Document.Infrastructure.Persistence;
using EFCore.NamingConventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace Microsoft.Extensions.DependencyInjection;

public static class DocumentInfrastructureModuleExtensions
{
    public static IServiceCollection AddDocumentInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISystemClock, SystemClock>();

        services.AddDbContext<DocumentDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString("Document")
                ?? configuration.GetConnectionString("postgres")
                ?? throw new InvalidOperationException("Document database connection string is not configured.");

            options.UseNpgsql(
                    connectionString,
                    builder => builder.MigrationsAssembly(typeof(DocumentDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IDocumentRepository, DocumentRepository>();

        return services;
    }
}
