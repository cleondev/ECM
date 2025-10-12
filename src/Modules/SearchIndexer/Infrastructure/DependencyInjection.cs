using ECM.SearchIndexer.Application.Indexing.Abstractions;
using ECM.SearchIndexer.Infrastructure.Indexing;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.SearchIndexer.Infrastructure;

public static class SearchIndexerInfrastructureModuleExtensions
{
    public static IServiceCollection AddSearchIndexerInfrastructure(this IServiceCollection services)
    {
        services.AddHangfire(configuration => configuration.UseMemoryStorage());
        services.AddHangfireServer(options =>
        {
            options.ServerName = "search-indexer";
        });

        services.AddSingleton<InMemorySearchIndexStore>();
        services.AddSingleton<ISearchIndexWriter>(provider => provider.GetRequiredService<InMemorySearchIndexStore>());
        services.AddSingleton<ISearchIndexReader>(provider => provider.GetRequiredService<InMemorySearchIndexStore>());
        services.AddSingleton<SearchIndexingJob>();
        services.AddSingleton<IIndexingJobScheduler, HangfireIndexingJobScheduler>();

        return services;
    }
}
