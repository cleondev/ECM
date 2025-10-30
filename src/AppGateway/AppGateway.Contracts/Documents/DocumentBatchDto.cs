using System.Collections.Generic;

namespace AppGateway.Contracts.Documents;

public sealed record DocumentBatchDto(
    IReadOnlyCollection<DocumentDto> Documents,
    IReadOnlyCollection<DocumentUploadFailureDto> Failures);
