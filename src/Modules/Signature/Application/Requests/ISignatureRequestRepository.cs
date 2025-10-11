using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.Signature.Domain.Requests;

namespace ECM.Signature.Application.Requests;

public interface ISignatureRequestRepository
{
    Task AddAsync(SignatureRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SignatureRequest>> GetPendingAsync(CancellationToken cancellationToken = default);
}
