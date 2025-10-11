using System;

namespace ECM.BuildingBlocks.Domain.Events;

public interface IDomainEvent
{
    DateTimeOffset OccurredAtUtc { get; }
}
