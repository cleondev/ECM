using System;
using ECM.BuildingBlocks.Infrastructure.Caching.Internal;
using Enyim.Caching.Configuration;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;

namespace ECM.BuildingBlocks.Infrastructure.Caching;

public static class CacheServiceCollectionExtensions
{
    public static IServiceCollection AddConfiguredCache(this IServiceCollection services, CacheOptions cacheOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(cacheOptions);

        return cacheOptions.Mode switch
        {
            CacheMode.Memory => services.AddMemoryCache(cacheOptions).AddSingleton<ICacheProvider, MemoryCacheProvider>(),
            CacheMode.Redis => services.AddRedisCache(cacheOptions).AddSingleton<ICacheProvider, DistributedCacheProvider>(),
            CacheMode.Memcached => services.AddMemcachedCache(cacheOptions).AddSingleton<ICacheProvider, MemcachedCacheProvider>(),
            _ => services.AddSingleton<ICacheProvider, NoCacheProvider>()
        };
    }

    private static IServiceCollection AddMemoryCache(this IServiceCollection services, CacheOptions cacheOptions)
    {
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = cacheOptions.Memory.SizeLimit;
        });

        return services;
    }

    private static IServiceCollection AddRedisCache(this IServiceCollection services, CacheOptions cacheOptions)
    {
        if (string.IsNullOrWhiteSpace(cacheOptions.Redis.Configuration))
        {
            throw new InvalidOperationException("Redis cache mode is enabled but no configuration string was provided.");
        }

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = cacheOptions.Redis.Configuration;
            options.InstanceName = cacheOptions.Redis.InstanceName;
        });

        return services;
    }

    private static IServiceCollection AddMemcachedCache(this IServiceCollection services, CacheOptions cacheOptions)
    {
        if (cacheOptions.Memcached.Servers.Count == 0)
        {
            throw new InvalidOperationException("Memcached cache mode is enabled but no servers were configured.");
        }

        services.AddEnyimMemcached(options =>
        {
            foreach (var server in cacheOptions.Memcached.Servers)
            {
                options.AddServer(server.Host, server.Port);
            }
        });

        return services;
    }
}
