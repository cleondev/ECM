using System;
using System.Collections.Generic;
using System.Linq;

namespace AppGateway.Api.Fake;

internal static class FakeEcmData
{
    private static readonly object SyncRoot = new();

    private static readonly Dictionary<Guid, IReadOnlyList<FakeFlowResponse>> FlowIndex = new();

    private static readonly IReadOnlyList<FakeFlowResponse> FlowTemplates =
    [
        new(
            "flow-design-review",
            "Design Review",
            "active",
            "2 hours ago",
            "Feedback received",
            new List<FakeFlowStepResponse>
            {
                new(
                    "step-feedback",
                    "Feedback received",
                    "Design team provided feedback on v2",
                    "2 hours ago",
                    "Design Team",
                    "User",
                    "text-primary"),
                new(
                    "step-review",
                    "Review requested",
                    "Submitted dashboard v2 for review",
                    "1 day ago",
                    "John Doe",
                    "GitBranch",
                    "text-blue-500")
            }),
        new(
            "flow-version-control",
            "Version Control",
            "completed",
            "3 days ago",
            "Version 2 created",
            new List<FakeFlowStepResponse>
            {
                new(
                    "step-version-created",
                    "Version 2 created",
                    "Created new version with updated metrics",
                    "3 days ago",
                    "John Doe",
                    "FileText",
                    "text-green-500"),
                new(
                    "step-archived",
                    "Version 1 archived",
                    "Moved v1 to archive folder",
                    "3 days ago",
                    "John Doe",
                    "FolderOpen",
                    "text-gray-500")
            })
    ];

    private static readonly Dictionary<Guid, FakeWorkflowInstanceResponse> WorkflowInstances = new();

    private static readonly List<FakeWorkflowInstanceResponse> WorkflowTemplates =
    [
        new(
            Guid.Parse("8c9bfd4a-8b8f-4741-966c-1ce08788ab96"),
            Guid.Parse("2ed1f3c8-04a2-45a1-80bc-2ae6f7a040f4"),
            "Standard approval",
            Guid.Empty,
            "active",
            DateTimeOffset.UtcNow.AddHours(-6),
            DateTimeOffset.UtcNow.AddMinutes(-45),
            new List<FakeWorkflowInstanceStepResponse>
            {
                new(
                    "step-assign",
                    "Assign reviewer",
                    "Sara Weaver",
                    "completed",
                    DateTimeOffset.UtcNow.AddHours(-6),
                    DateTimeOffset.UtcNow.AddHours(-5),
                    DateTimeOffset.UtcNow.AddHours(-5),
                    "Assigned to design lead"),
                new(
                    "step-reviewing",
                    "Review in progress",
                    "Design Team",
                    "in-progress",
                    DateTimeOffset.UtcNow.AddHours(-5),
                    DateTimeOffset.UtcNow.AddMinutes(-45),
                    null,
                    "Reviewing dashboard layout")
            })
    ];

    internal static IReadOnlyList<FakeFlowResponse> GetFlows(Guid documentId)
    {
        lock (SyncRoot)
        {
            if (FlowIndex.TryGetValue(documentId, out var flows))
            {
                return flows;
            }

            flows = FlowTemplates
                .Select(flow => flow with { Id = $"{flow.Id}-{documentId.ToString()[..8]}" })
                .ToList();

            FlowIndex[documentId] = flows;
            return flows;
        }
    }

    internal static IReadOnlyList<FakeWorkflowInstanceResponse> GetWorkflowInstances(Guid documentId)
    {
        lock (SyncRoot)
        {
            EnsureWorkflowInstance(documentId);

            return WorkflowInstances
                .Values
                .Where(instance => instance.DocumentId == documentId)
                .Select(instance => instance with { UpdatedAtUtc = DateTimeOffset.UtcNow })
                .ToList();
        }
    }

    internal static FakeWorkflowInstanceResponse? GetWorkflowInstance(Guid instanceId, Guid? documentId)
    {
        lock (SyncRoot)
        {
            if (!WorkflowInstances.TryGetValue(instanceId, out var instance))
            {
                return null;
            }

            if (documentId.HasValue && instance.DocumentId != documentId.Value)
            {
                return null;
            }

            return instance with { UpdatedAtUtc = DateTimeOffset.UtcNow };
        }
    }

    private static void EnsureWorkflowInstance(Guid documentId)
    {
        if (WorkflowInstances.Values.Any(instance => instance.DocumentId == documentId))
        {
            return;
        }

        var template = WorkflowTemplates.First();
        var instanceId = Guid.NewGuid();
        var steps = template.Steps
            .Select(step => step with { Id = $"{step.Id}-{instanceId.ToString()[..8]}" })
            .ToList();

        WorkflowInstances[instanceId] = template with
        {
            Id = instanceId,
            DocumentId = documentId,
            StartedAtUtc = DateTimeOffset.UtcNow.AddHours(-2),
            UpdatedAtUtc = DateTimeOffset.UtcNow.AddMinutes(-10),
            Steps = steps,
        };
    }
}

internal sealed record FakeFlowStepResponse(
    string Id,
    string Title,
    string Description,
    string Timestamp,
    string User,
    string Icon,
    string IconColor);

internal sealed record FakeFlowResponse(
    string Id,
    string Name,
    string Status,
    string LastUpdated,
    string LastStep,
    IReadOnlyList<FakeFlowStepResponse> Steps);

internal sealed record FakeWorkflowInstanceStepResponse(
    string Id,
    string Name,
    string? Assignee,
    string? Status,
    DateTimeOffset? CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc,
    DateTimeOffset? CompletedAtUtc,
    string? Notes);

internal sealed record FakeWorkflowInstanceResponse(
    Guid Id,
    Guid DefinitionId,
    string DefinitionName,
    Guid DocumentId,
    string State,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<FakeWorkflowInstanceStepResponse> Steps);
