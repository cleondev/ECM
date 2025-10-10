using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.Signature.Domain.Requests;

namespace ECM.Signature.Application.Requests;

public sealed class SignatureApplicationService(ISignatureRequestRepository repository)
{
    private readonly ISignatureRequestRepository _repository = repository;

    public async Task<OperationResult<SignatureRequest>> CreateAsync(CreateSignatureRequestCommand command, CancellationToken cancellationToken)
    {
        var result = command.ToDomain();
        if (result.IsFailure || result.Value is null)
        {
            return result;
        }

        await _repository.AddAsync(result.Value, cancellationToken);
        return result;
    }

    public Task<IReadOnlyCollection<SignatureRequest>> GetPendingAsync(CancellationToken cancellationToken)
        => _repository.GetPendingAsync(cancellationToken);
}
