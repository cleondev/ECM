using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.IAM.Domain.Relations;

namespace ECM.IAM.Application.Relations.Commands;

public sealed class DeleteAccessRelationCommandHandler(IAccessRelationRepository repository, ISystemClock clock)
{
    private readonly IAccessRelationRepository _repository = repository;
    private readonly ISystemClock _clock = clock;

    public async Task<bool> HandleAsync(DeleteAccessRelationCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedSubjectType = command.SubjectType?.Trim().ToLowerInvariant() ?? string.Empty;

        var relation = await _repository.GetAsync(
            normalizedSubjectType,
            command.SubjectId,
            command.ObjectType,
            command.ObjectId,
            command.Relation,
            cancellationToken);
        if (relation is null)
        {
            return false;
        }

        relation.MarkDeleted(_clock.UtcNow);
        await _repository.DeleteAsync(relation, cancellationToken);
        return true;
    }
}
