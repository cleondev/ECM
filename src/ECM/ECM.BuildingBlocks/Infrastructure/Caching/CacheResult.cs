using System.Diagnostics.CodeAnalysis;

namespace ECM.BuildingBlocks.Infrastructure.Caching;

public readonly struct CacheResult<T>
{
    public CacheResult(bool found, [MaybeNull] T value)
    {
        Found = found;
        Value = value;
    }

    public bool Found { get; }

    [MaybeNull]
    public T Value { get; }

    public static CacheResult<T> NotFound() => new(false, default!);
}
