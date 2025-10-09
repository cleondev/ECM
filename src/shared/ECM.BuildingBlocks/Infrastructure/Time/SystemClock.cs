using ECM.BuildingBlocks.Application.Abstractions.Time;

namespace ECM.BuildingBlocks.Infrastructure.Time;

public sealed class SystemClock : ISystemClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
