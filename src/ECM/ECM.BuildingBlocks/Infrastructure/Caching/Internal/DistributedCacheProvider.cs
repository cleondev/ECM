using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace ECM.BuildingBlocks.Infrastructure.Caching.Internal;

internal sealed class DistributedCacheProvider : CacheProviderBase
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IDistributedCache _distributedCache;

    public DistributedCacheProvider(IDistributedCache distributedCache, IOptions<CacheOptions> options)
        : base(options)
    {
        _distributedCache = distributedCache;
    }

    public override async Task<CacheResult<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var cached = await _distributedCache.GetStringAsync(key, cancellationToken).ConfigureAwait(false);
        if (cached is null)
        {
            return CacheResult<T>.NotFound();
        }

        try
        {
            var value = JsonSerializer.Deserialize<T>(cached, SerializerOptions);
            return new CacheResult<T>(true, value!);
        }
        catch (JsonException)
        {
            await _distributedCache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            return CacheResult<T>.NotFound();
        }
    }

    protected override Task SetInternalAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken cancellationToken)
    {
        var payload = JsonSerializer.Serialize(value, SerializerOptions);
        var cacheOptions = new DistributedCacheEntryOptions();
        if (options.AbsoluteExpirationRelativeToNow is { } absolute)
        {
            cacheOptions.SetAbsoluteExpiration(absolute);
        }

        if (options.SlidingExpiration is { } sliding)
        {
            cacheOptions.SetSlidingExpiration(sliding);
        }

        return _distributedCache.SetStringAsync(key, payload, cacheOptions, cancellationToken);
    }

    public override Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return _distributedCache.RemoveAsync(key, cancellationToken);
    }
}
