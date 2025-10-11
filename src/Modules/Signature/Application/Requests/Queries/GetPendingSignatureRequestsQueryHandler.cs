using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.Signature.Domain.Requests;

namespace ECM.Signature.Application.Requests.Queries;

public sealed class GetPendingSignatureRequestsQueryHandler(ISignatureRequestRepository repository)
{
    private readonly ISignatureRequestRepository _repository = repository;

    public Task<IReadOnlyCollection<SignatureRequest>> HandleAsync(GetPendingSignatureRequestsQuery query, CancellationToken cancellationToken)
        => _repository.GetPendingAsync(cancellationToken);
}
