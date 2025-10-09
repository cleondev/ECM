using ECM.Modules.SearchRead.Application.Search;

namespace Microsoft.Extensions.DependencyInjection;

public static class SearchReadApplicationModuleExtensions
{
    public static IServiceCollection AddSearchReadApplication(this IServiceCollection services)
    {
        services.AddScoped<SearchApplicationService>();
        return services;
    }
}
