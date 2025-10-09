using System.Net.Http.Json;
using AppGateway.Contracts.Documents;
using AppGateway.Contracts.Signatures;
using AppGateway.Contracts.Workflows;

namespace AppGateway.Infrastructure.Ecm;

internal sealed class EcmApiClient(HttpClient httpClient) : IEcmApiClient
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<IReadOnlyCollection<DocumentSummaryDto>> GetDocumentsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetFromJsonAsync<IReadOnlyCollection<DocumentSummaryDto>>("api/ecm/documents", cancellationToken);
        return response ?? Array.Empty<DocumentSummaryDto>();
    }

    public async Task<DocumentSummaryDto?> CreateDocumentAsync(CreateDocumentRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/ecm/documents", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<DocumentSummaryDto>(cancellationToken: cancellationToken);
    }

    public async Task<WorkflowInstanceDto?> StartWorkflowAsync(StartWorkflowRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/ecm/workflows/instances", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<WorkflowInstanceDto>(cancellationToken: cancellationToken);
    }

    public async Task<SignatureReceiptDto?> CreateSignatureRequestAsync(SignatureRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync("api/ecm/signatures", request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        return await response.Content.ReadFromJsonAsync<SignatureReceiptDto>(cancellationToken: cancellationToken);
    }
}
