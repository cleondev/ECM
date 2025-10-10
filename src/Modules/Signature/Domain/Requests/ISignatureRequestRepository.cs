using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ECM.Signature.Domain.Requests;

public interface ISignatureRequestRepository
{
    Task AddAsync(SignatureRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SignatureRequest>> GetPendingAsync(CancellationToken cancellationToken = default);
}
