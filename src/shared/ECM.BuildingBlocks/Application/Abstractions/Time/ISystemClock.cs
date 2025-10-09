namespace ECM.BuildingBlocks.Application.Abstractions.Time;

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}
