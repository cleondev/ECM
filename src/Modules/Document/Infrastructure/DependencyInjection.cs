using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.BuildingBlocks.Infrastructure.Time;
using ECM.Document.Domain.Documents;
using ECM.Document.Infrastructure.Documents;
using ECM.Document.Infrastructure.Persistence;
using EFCore.NamingConventions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace ECM.Document.Infrastructure;

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
