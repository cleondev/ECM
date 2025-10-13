using System;

namespace ECM.BuildingBlocks.Infrastructure.Caching;

public sealed class CacheEntryOptions
{
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

    public TimeSpan? SlidingExpiration { get; set; }

    public CacheEntryOptions Clone()
    {
        return new CacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = AbsoluteExpirationRelativeToNow,
            SlidingExpiration = SlidingExpiration
        };
    }
}
