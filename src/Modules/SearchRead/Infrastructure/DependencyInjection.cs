using ECM.SearchRead.Application.Search.Abstractions;
using ECM.SearchRead.Infrastructure.Search;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.SearchRead.Infrastructure;

public static class SearchReadInfrastructureModuleExtensions
{
    public static IServiceCollection AddSearchReadInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ISearchReadProvider, InMemorySearchReadProvider>();
        return services;
    }
}
