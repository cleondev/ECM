using System;
using System.Collections.Generic;

namespace ECM.Workflow.Domain.Tasks;

public sealed class WorkflowTask(
    string id,
    Guid? documentId,
    string name,
    string definitionKey,
    string instanceId,
    string? assigneeId,
    WorkflowTaskState state,
    DateTimeOffset createdAtUtc,
    string? formKey,
    IReadOnlyDictionary<string, object?>? variables)
{
    public string Id { get; } = id;

    public Guid? DocumentId { get; } = documentId;

    public string Name { get; } = name;

    public string DefinitionKey { get; } = definitionKey;

    public string InstanceId { get; } = instanceId;

    public string? AssigneeId { get; private set; } = assigneeId;

    public WorkflowTaskState State { get; private set; } = state;

    public DateTimeOffset CreatedAtUtc { get; } = createdAtUtc;

    public string? FormKey { get; } = formKey;

    public IReadOnlyDictionary<string, object?>? Variables { get; private set; } = variables;

    public void Assign(string assigneeId)
    {
        AssigneeId = assigneeId;
        State = WorkflowTaskState.Open;
    }

    public void MarkCompleted()
    {
        State = WorkflowTaskState.Completed;
    }

    public void UpdateVariables(IReadOnlyDictionary<string, object?> variables)
    {
        Variables = variables;
    }
}
