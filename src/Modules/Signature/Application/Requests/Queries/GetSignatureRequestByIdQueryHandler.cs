using System.Threading;
using System.Threading.Tasks;
using ECM.Signature.Application.Requests;
using ECM.Signature.Domain.Requests;

namespace ECM.Signature.Application.Requests.Queries;

public sealed class GetSignatureRequestByIdQueryHandler(ISignatureRequestRepository repository)
{
    private readonly ISignatureRequestRepository _repository = repository;

    public Task<SignatureRequest?> HandleAsync(GetSignatureRequestByIdQuery query, CancellationToken cancellationToken)
        => _repository.FindAsync(query.Id, cancellationToken);
}
