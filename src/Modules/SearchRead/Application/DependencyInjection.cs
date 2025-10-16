using ECM.SearchRead.Application.Search;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.SearchRead.Application;

public static class SearchReadApplicationModuleExtensions
{
    public static IServiceCollection AddSearchReadApplication(this IServiceCollection services)
    {
        services.AddScoped<SearchQueryHandler>();
        services.AddScoped<SuggestQueryHandler>();
        services.AddScoped<SearchFacetsQueryHandler>();
        return services;
    }
}
