namespace AppGateway.Contracts.Workflows;

public sealed record WorkflowInstanceDto(Guid Id, Guid DocumentId, string Definition, string Status);
