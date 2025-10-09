using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.BuildingBlocks.Infrastructure.Time;
using ECM.Modules.Document.Domain.Documents;
using ECM.Modules.Document.Infrastructure.Documents;

namespace Microsoft.Extensions.DependencyInjection;

public static class DocumentInfrastructureModuleExtensions
{
    public static IServiceCollection AddDocumentInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
        return services;
    }
}
