using AppGateway.Contracts.Documents;
using AppGateway.Contracts.Signatures;
using AppGateway.Contracts.Workflows;

namespace AppGateway.Infrastructure.Ecm;

public interface IEcmApiClient
{
    Task<IReadOnlyCollection<DocumentSummaryDto>> GetDocumentsAsync(CancellationToken cancellationToken = default);

    Task<DocumentSummaryDto?> CreateDocumentAsync(CreateDocumentRequestDto request, CancellationToken cancellationToken = default);

    Task<WorkflowInstanceDto?> StartWorkflowAsync(StartWorkflowRequestDto request, CancellationToken cancellationToken = default);

    Task<SignatureReceiptDto?> CreateSignatureRequestAsync(SignatureRequestDto request, CancellationToken cancellationToken = default);
}
