using Ecm.Application.Abstractions.Time;
using Ecm.Domain.Documents;
using Ecm.Infrastructure.Documents;
using Ecm.Infrastructure.Time;

namespace Microsoft.Extensions.DependencyInjection;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISystemClock, SystemClock>();
        services.AddSingleton<IDocumentRepository, InMemoryDocumentRepository>();
        return services;
    }
}
