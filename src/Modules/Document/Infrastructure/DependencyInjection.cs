using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.BuildingBlocks.Infrastructure.Configuration;
using ECM.BuildingBlocks.Infrastructure.Time;
using ECM.Document.Application.Documents.AccessControl;
using ECM.Document.Application.Documents.Queries;
using ECM.Document.Application.Documents.Repositories;
using ECM.Document.Application.Shares;
using ECM.Document.Application.Tags.Repositories;
using ECM.Document.Infrastructure.AccessControl;
using ECM.Document.Infrastructure.Documents;
using ECM.Document.Infrastructure.Documents.Queries;
using ECM.Document.Infrastructure.Tags;
using ECM.Document.Infrastructure.Persistence;
using ECM.Document.Infrastructure.Shares;
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

        services.AddOptions<ShareLinkOptions>()
            .BindConfiguration(ShareLinkOptions.SectionName)
            .ValidateOnStart();

        services.AddDbContext<DocumentDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetRequiredConnectionStringForModule("Document");

            options.UseNpgsql(
                    connectionString,
                    builder => builder.MigrationsAssembly(typeof(DocumentDbContext).Assembly.FullName))
                .UseSnakeCaseNamingConvention();
        });

        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentVersionReadService, DocumentVersionReadService>();
        services.AddScoped<ITagLabelRepository, TagLabelRepository>();
        services.AddScoped<ITagNamespaceRepository, TagNamespaceRepository>();
        services.AddScoped<IEffectiveAclFlatWriter, EffectiveAclFlatWriter>();
        services.AddScoped<IShareLinkRepository, ShareLinkRepository>();
        services.AddSingleton<ISharePasswordHasher, Argon2SharePasswordHasher>();

        return services;
    }
}
