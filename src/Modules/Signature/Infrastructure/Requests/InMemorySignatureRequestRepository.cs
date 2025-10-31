using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.Signature.Application.Requests;
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

    public Task<IReadOnlyCollection<SignatureRequest>> GetAsync(SignatureRequestQuery query, CancellationToken cancellationToken = default)
    {
        var items = _requests.Values.AsEnumerable();

        if (query.Status.HasValue)
        {
            items = items.Where(request => request.Status == query.Status);
        }

        return Task.FromResult<IReadOnlyCollection<SignatureRequest>>([.. items]);
    }

    public Task<SignatureRequest?> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _requests.TryGetValue(id, out var request);
        return Task.FromResult(request);
    }

    public Task UpdateAsync(SignatureRequest request, CancellationToken cancellationToken = default)
    {
        _requests[request.Id] = request;
        return Task.CompletedTask;
    }
}
