using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.Signature.Domain.Requests;

namespace ECM.Signature.Application.Requests;

public interface ISignatureRequestRepository
{
    Task AddAsync(SignatureRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<SignatureRequest>> GetAsync(SignatureRequestQuery query, CancellationToken cancellationToken = default);

    Task<SignatureRequest?> FindAsync(Guid id, CancellationToken cancellationToken = default);

    Task UpdateAsync(SignatureRequest request, CancellationToken cancellationToken = default);
}
