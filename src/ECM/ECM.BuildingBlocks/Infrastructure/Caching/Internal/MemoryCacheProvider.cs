using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace ECM.BuildingBlocks.Infrastructure.Caching.Internal;

internal sealed class MemoryCacheProvider(IMemoryCache memoryCache, IOptions<CacheOptions> options) : CacheProviderBase(options)
{
    private readonly IMemoryCache _memoryCache = memoryCache;

    public override Task<CacheResult<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        if (_memoryCache.TryGetValue(key, out var cached) && cached is T value)
        {
            return Task.FromResult(new CacheResult<T>(true, value));
        }

        return Task.FromResult(CacheResult<T>.NotFound());
    }

    protected override Task SetInternalAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken cancellationToken)
    {
        var entryOptions = new MemoryCacheEntryOptions();
        if (options.AbsoluteExpirationRelativeToNow is { } absolute)
        {
            entryOptions.SetAbsoluteExpiration(absolute);
        }

        if (options.SlidingExpiration is { } sliding)
        {
            entryOptions.SetSlidingExpiration(sliding);
        }

        _memoryCache.Set(key, value!, entryOptions);
        return Task.CompletedTask;
    }

    public override Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _memoryCache.Remove(key);
        return Task.CompletedTask;
    }
}
