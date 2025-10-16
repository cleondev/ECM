using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.Signature.Application.Requests;

namespace ECM.Signature.Application.Requests.Commands;

public sealed class CancelSignatureRequestCommandHandler(ISignatureRequestRepository repository)
{
    private readonly ISignatureRequestRepository _repository = repository;

    public async Task<OperationResult> HandleAsync(CancelSignatureRequestCommand command, CancellationToken cancellationToken)
    {
        var request = await _repository.FindAsync(command.RequestId, cancellationToken).ConfigureAwait(false);
        if (request is null)
        {
            return OperationResult.Failure("Signature request not found.");
        }

        request.Cancel(DateTimeOffset.UtcNow);
        await _repository.UpdateAsync(request, cancellationToken).ConfigureAwait(false);
        return OperationResult.Success();
    }
}
