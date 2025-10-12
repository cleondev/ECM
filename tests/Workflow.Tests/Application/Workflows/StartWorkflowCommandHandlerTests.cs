using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.Workflow.Application.Workflows;
using ECM.Workflow.Application.Workflows.Commands;
using ECM.Workflow.Domain.Definitions;
using ECM.Workflow.Domain.Instances;
using Xunit;

namespace Workflow.Tests.Application.Workflows;

public class StartWorkflowCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_StartsWorkflowThroughRepository()
    {
        var repository = new FakeWorkflowRepository();
        var handler = new StartWorkflowCommandHandler(repository);
        var command = new StartWorkflowCommand(Guid.NewGuid(), "definition-key");
        var definition = new WorkflowDefinition("definition-id", "definition-key", "Sample", 1);
        var workflow = new WorkflowInstance(Guid.NewGuid(), command.DocumentId, definition, WorkflowStatus.Running, DateTimeOffset.UtcNow, "ext-1");
        repository.StartResult = OperationResult<WorkflowInstance>.Success(workflow);

        var cancellationTokenSource = new CancellationTokenSource();
        var result = await handler.HandleAsync(command, cancellationTokenSource.Token);

        Assert.Same(repository.StartResult, result);
        Assert.Equal(command.DocumentId, repository.StartDocumentId);
        Assert.Equal(command.Definition, repository.StartDefinitionKey);
        Assert.Equal(cancellationTokenSource.Token, repository.StartCancellationToken);
        Assert.Same(workflow, result.Value);
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
