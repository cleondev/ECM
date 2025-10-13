using ECM.SearchIndexer.Application.Events;
using ECM.SearchIndexer.Application.Indexing;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.SearchIndexer.Application;

public static class SearchIndexerApplicationModuleExtensions
{
    public static IServiceCollection AddSearchIndexerApplication(this IServiceCollection services)
    {
        services.AddScoped<EnqueueDocumentIndexingHandler>();
        services.AddScoped<SearchIndexingEventProcessor>();
        return services;
    }
}
