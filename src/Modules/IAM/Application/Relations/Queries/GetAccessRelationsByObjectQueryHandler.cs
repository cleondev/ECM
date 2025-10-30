using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.IAM.Application.Relations;

namespace ECM.IAM.Application.Relations.Queries;

public sealed class GetAccessRelationsByObjectQueryHandler(IAccessRelationRepository repository)
{
    private readonly IAccessRelationRepository _repository = repository;

    public async Task<IReadOnlyCollection<AccessRelationSummaryResult>> HandleAsync(GetAccessRelationsByObjectQuery query, CancellationToken cancellationToken = default)
    {
        var relations = await _repository.GetByObjectAsync(query.ObjectType, query.ObjectId, cancellationToken);
        return [.. relations.Select(relation => relation.ToResult())];
    }
}
