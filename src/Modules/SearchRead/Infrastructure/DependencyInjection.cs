using ECM.Modules.SearchRead.Infrastructure.Search;

namespace Microsoft.Extensions.DependencyInjection;

public static class SearchReadInfrastructureModuleExtensions
{
    public static IServiceCollection AddSearchReadInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISearchReadProvider, InMemorySearchReadProvider>();
        return services;
    }
}
