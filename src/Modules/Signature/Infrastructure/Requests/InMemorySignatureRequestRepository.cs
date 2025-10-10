using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.Signature.Domain.Requests;

namespace ECM.Signature.Infrastructure.Requests;

internal sealed class InMemorySignatureRequestRepository : ISignatureRequestRepository
{
    private readonly ConcurrentDictionary<Guid, SignatureRequest> _requests = new();

    public Task AddAsync(SignatureRequest request, CancellationToken cancellationToken = default)
    {
        _requests[request.Id] = request;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<SignatureRequest>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        var pending = _requests.Values
            .Where(request => request.Status is SignatureStatus.Pending)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<SignatureRequest>>(pending);
    }
}
