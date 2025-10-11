using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.AccessControl.Application.Relations;

namespace ECM.AccessControl.Application.Relations.Queries;

public sealed class GetAccessRelationsBySubjectQueryHandler(IAccessRelationRepository repository)
{
    private readonly IAccessRelationRepository _repository = repository;

    public async Task<IReadOnlyCollection<AccessRelationSummary>> HandleAsync(GetAccessRelationsBySubjectQuery query, CancellationToken cancellationToken = default)
    {
        var relations = await _repository.GetBySubjectAsync(query.SubjectId, cancellationToken);
        return relations.Select(AccessRelationSummaryMapper.ToSummary).ToArray();
    }
}
