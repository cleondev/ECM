using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;

namespace ECM.BuildingBlocks.Infrastructure.Caching.Internal;

internal abstract class CacheProviderBase(IOptions<CacheOptions> options) : ICacheProvider
{
    private readonly CacheEntryOptions _defaultEntryOptions = options.Value.DefaultEntryOptions?.Clone() ?? new CacheEntryOptions();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

    protected CacheEntryOptions GetEffectiveOptions(CacheEntryOptions? options)
    {
        if (options is null)
        {
            return _defaultEntryOptions.Clone();
        }

        var effective = _defaultEntryOptions.Clone();
        if (options.AbsoluteExpirationRelativeToNow is not null)
        {
            effective.AbsoluteExpirationRelativeToNow = options.AbsoluteExpirationRelativeToNow;
        }

        if (options.SlidingExpiration is not null)
        {
            effective.SlidingExpiration = options.SlidingExpiration;
        }

        return effective;
    }

    public abstract Task<CacheResult<T>> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        var existing = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
        if (existing.Found)
        {
            return existing.Value!;
        }

        var semaphore = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            existing = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
            if (existing.Found)
            {
                return existing.Value!;
            }

            var created = await factory(cancellationToken).ConfigureAwait(false);
            if (created is null)
            {
                await RemoveAsync(key, cancellationToken).ConfigureAwait(false);
                return created!;
            }

            await SetInternalAsync(key, created, GetEffectiveOptions(options), cancellationToken).ConfigureAwait(false);
            return created!;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        if (value is null)
        {
            await RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            return;
        }

        await SetInternalAsync(key, value, GetEffectiveOptions(options), cancellationToken).ConfigureAwait(false);
    }

    protected abstract Task SetInternalAsync<T>(string key, T value, CacheEntryOptions options, CancellationToken cancellationToken);

    public abstract Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
