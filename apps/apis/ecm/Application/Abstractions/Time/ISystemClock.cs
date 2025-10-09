namespace Ecm.Application.Abstractions.Time;

public interface ISystemClock
{
    DateTimeOffset UtcNow { get; }
}
