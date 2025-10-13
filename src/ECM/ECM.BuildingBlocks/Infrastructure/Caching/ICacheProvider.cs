using System;
using System.Threading;
using System.Threading.Tasks;

namespace ECM.BuildingBlocks.Infrastructure.Caching;

public interface ICacheProvider
{
    Task<CacheResult<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CacheEntryOptions? options = null, CancellationToken cancellationToken = default);

    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default);
}
