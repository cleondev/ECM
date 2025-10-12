using System;
using ECM.Workflow.Domain.Definitions;

namespace ECM.Workflow.Domain.Instances;

public sealed class WorkflowInstance
{
    public WorkflowInstance(Guid id, Guid documentId, WorkflowDefinition definition, WorkflowStatus status, DateTimeOffset startedAtUtc, string externalId)
    {
        Id = id;
        DocumentId = documentId;
        Definition = definition;
        Status = status;
        StartedAtUtc = startedAtUtc;
        ExternalId = externalId;
    }

    public Guid Id { get; }

    public Guid DocumentId { get; }

    public WorkflowDefinition Definition { get; }

    public WorkflowStatus Status { get; private set; }

    public DateTimeOffset StartedAtUtc { get; }

    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public string ExternalId { get; }

    public void MarkCompleted(DateTimeOffset completedAtUtc)
    {
        Status = WorkflowStatus.Completed;
        CompletedAtUtc = completedAtUtc;
    }
}
