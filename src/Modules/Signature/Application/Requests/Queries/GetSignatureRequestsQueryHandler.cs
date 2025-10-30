using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.Signature.Application.Requests;
using ECM.Signature.Domain.Requests;

namespace ECM.Signature.Application.Requests.Queries;

public sealed class GetSignatureRequestsQueryHandler(ISignatureRequestRepository repository)
{
    private readonly ISignatureRequestRepository _repository = repository;

    public Task<IReadOnlyCollection<SignatureRequest>> HandleAsync(GetSignatureRequestsQuery query, CancellationToken cancellationToken)
    {
        var filter = new SignatureRequestQuery(query.Status);
        return _repository.GetAsync(filter, cancellationToken);
    }
}
