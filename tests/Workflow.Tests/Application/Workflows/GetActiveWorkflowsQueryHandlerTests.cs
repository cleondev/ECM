using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.Workflow.Application.Workflows;
using ECM.Workflow.Application.Workflows.Queries;
using ECM.Workflow.Domain.Definitions;
using ECM.Workflow.Domain.Instances;
using Xunit;

namespace Workflow.Tests.Application.Workflows;

public class GetActiveWorkflowsQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_ReturnsActiveWorkflowsFromRepository()
    {
        var definition = new WorkflowDefinition("definition-id", "definition-key", "Sample", 1);
        var workflow = new WorkflowInstance(Guid.NewGuid(), Guid.NewGuid(), definition, WorkflowStatus.Running, DateTimeOffset.UtcNow, "ext-1");
        var repository = new FakeWorkflowRepository
        {
            ActiveWorkflows = new[] { workflow }
        };
        var handler = new GetActiveWorkflowsQueryHandler(repository);
        var query = new GetActiveWorkflowsQuery();
        var cancellationTokenSource = new CancellationTokenSource();

        var result = await handler.HandleAsync(query, cancellationTokenSource.Token);

        Assert.Equal(repository.ActiveWorkflows, result);
        Assert.Equal(cancellationTokenSource.Token, repository.GetActiveCancellationToken);
    }

    private sealed class FakeWorkflowRepository : IWorkflowRepository
    {
        public Guid StartDocumentId { get; private set; }

        public string? StartDefinitionKey { get; private set; }

        public CancellationToken StartCancellationToken { get; private set; }

        public OperationResult<WorkflowInstance>? StartResult { get; set; }

        public CancellationToken GetActiveCancellationToken { get; private set; }

        public IReadOnlyCollection<WorkflowInstance> ActiveWorkflows { get; set; } = Array.Empty<WorkflowInstance>();

        public Task<IReadOnlyCollection<WorkflowInstance>> GetActiveAsync(CancellationToken cancellationToken = default)
        {
            GetActiveCancellationToken = cancellationToken;
            return Task.FromResult(ActiveWorkflows);
        }

        public Task<OperationResult<WorkflowInstance>> StartAsync(Guid documentId, string definitionKey, CancellationToken cancellationToken = default)
        {
            StartDocumentId = documentId;
            StartDefinitionKey = definitionKey;
            StartCancellationToken = cancellationToken;
            return Task.FromResult(StartResult ?? OperationResult<WorkflowInstance>.Failure("Start result not configured"));
        }
    }
}
