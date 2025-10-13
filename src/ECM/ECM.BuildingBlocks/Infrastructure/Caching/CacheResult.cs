using System.Diagnostics.CodeAnalysis;

namespace ECM.BuildingBlocks.Infrastructure.Caching;

public readonly struct CacheResult<T>(bool found, [MaybeNull] T value)
{
    public bool Found { get; } = found;

    [MaybeNull]
    public T Value { get; } = value;

    public static CacheResult<T> NotFound() => new(false, default!);
}
