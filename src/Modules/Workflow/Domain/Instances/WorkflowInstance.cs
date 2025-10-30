using System;
using ECM.Workflow.Domain.Definitions;

namespace ECM.Workflow.Domain.Instances;

public sealed class WorkflowInstance(Guid id, Guid documentId, WorkflowDefinition definition, WorkflowStatus status, DateTimeOffset startedAtUtc, string externalId)
{
    public Guid Id { get; } = id;

    public Guid DocumentId { get; } = documentId;

    public WorkflowDefinition Definition { get; } = definition;

    public WorkflowStatus Status { get; private set; } = status;

    public DateTimeOffset StartedAtUtc { get; } = startedAtUtc;

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public string ExternalId { get; } = externalId;

    public void MarkCompleted(DateTimeOffset completedAtUtc)
    {
        Status = WorkflowStatus.Completed;
        CompletedAtUtc = completedAtUtc;
    }

    public void MarkCancelled(DateTimeOffset cancelledAtUtc)
    {
        Status = WorkflowStatus.Cancelled;
        CompletedAtUtc = cancelledAtUtc;
    }
}
