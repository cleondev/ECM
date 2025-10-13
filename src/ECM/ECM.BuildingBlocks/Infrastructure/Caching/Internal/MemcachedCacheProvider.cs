using System;
using System.Threading;
using System.Threading.Tasks;
using Enyim.Caching;
using Enyim.Caching.Memcached;
using Microsoft.Extensions.Options;

namespace ECM.BuildingBlocks.Infrastructure.Caching.Internal;

internal sealed class MemcachedCacheProvider : CacheProviderBase
{
    private readonly IMemcachedClient _client;

    public MemcachedCacheProvider(IMemcachedClient client, IOptions<CacheOptions> options)
        : base(options)
    {
        _client = client;
    }

    public override async Task<CacheResult<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var result = await _client.GetAsync<T>(key).ConfigureAwait(false);
        if (result is { Success: true } casResult)
        {
            return new CacheResult<T>(true, casResult.Value);
        }

        return CacheResult<T>.NotFound();
    }

    protected override async Task SetInternalAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken cancellationToken)
    {
        var expiration = options.AbsoluteExpirationRelativeToNow ?? TimeSpan.FromMinutes(5);
        await _client.StoreAsync(StoreMode.Set, key, value!, expiration).ConfigureAwait(false);
    }

    public override Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return _client.RemoveAsync(key);
    }
}
