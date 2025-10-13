using System;
using System.Collections.Generic;

namespace ECM.BuildingBlocks.Infrastructure.Caching;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public CacheMode Mode { get; set; } = CacheMode.Off;

    public MemoryCacheOptions Memory { get; set; } = new();

    public RedisCacheOptions Redis { get; set; } = new();

    public MemcachedCacheOptions Memcached { get; set; } = new();

    public CacheEntryOptions DefaultEntryOptions { get; set; } = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };
}

public sealed class MemoryCacheOptions
{
    public long? SizeLimit { get; set; }
}

public sealed class RedisCacheOptions
{
    public string? Configuration { get; set; }

    public string? InstanceName { get; set; }
}

public sealed class MemcachedCacheOptions
{
    public List<MemcachedServerOptions> Servers { get; set; } =
    [
        new MemcachedServerOptions()
    ];
}

public sealed class MemcachedServerOptions
{
    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 11211;
}
