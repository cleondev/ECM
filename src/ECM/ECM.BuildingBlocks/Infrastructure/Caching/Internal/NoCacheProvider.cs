using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace ECM.BuildingBlocks.Infrastructure.Caching.Internal;

internal sealed class NoCacheProvider(IOptions<CacheOptions> options) : CacheProviderBase(options)
{
    public override Task<CacheResult<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(CacheResult<T>.NotFound());
    }

    protected override Task SetInternalAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public override Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
