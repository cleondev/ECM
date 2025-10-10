using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.AccessControl.Domain.Relations;

namespace ECM.AccessControl.Application.Relations;

public sealed class AccessRelationApplicationService(
    IAccessRelationRepository repository,
    ISystemClock clock)
{
    private readonly IAccessRelationRepository _repository = repository;
    private readonly ISystemClock _clock = clock;

    public async Task<IReadOnlyCollection<AccessRelationSummary>> GetBySubjectAsync(Guid subjectId, CancellationToken cancellationToken = default)
    {
        var relations = await _repository.GetBySubjectAsync(subjectId, cancellationToken);
        return relations.Select(MapToSummary).ToArray();
    }

    public async Task<IReadOnlyCollection<AccessRelationSummary>> GetByObjectAsync(string objectType, Guid objectId, CancellationToken cancellationToken = default)
    {
        var relations = await _repository.GetByObjectAsync(objectType, objectId, cancellationToken);
        return relations.Select(MapToSummary).ToArray();
    }

    public async Task<OperationResult<AccessRelationSummary>> CreateAsync(CreateAccessRelationCommand command, CancellationToken cancellationToken = default)
    {
        if (await _repository.GetAsync(command.SubjectId, command.ObjectType, command.ObjectId, command.Relation, cancellationToken) is not null)
        {
            return OperationResult<AccessRelationSummary>.Failure("The relation already exists.");
        }

        AccessRelation relation;
        try
        {
            relation = AccessRelation.Create(
                command.SubjectId,
                command.ObjectType,
                command.ObjectId,
                command.Relation,
                _clock.UtcNow);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<AccessRelationSummary>.Failure(exception.Message);
        }

        await _repository.AddAsync(relation, cancellationToken);

        return OperationResult<AccessRelationSummary>.Success(MapToSummary(relation));
    }

    public async Task<bool> DeleteAsync(DeleteAccessRelationCommand command, CancellationToken cancellationToken = default)
    {
        var relation = await _repository.GetAsync(command.SubjectId, command.ObjectType, command.ObjectId, command.Relation, cancellationToken);
        if (relation is null)
        {
            return false;
        }

        await _repository.DeleteAsync(relation, cancellationToken);
        return true;
    }

    private static AccessRelationSummary MapToSummary(AccessRelation relation)
        => new(
            relation.SubjectId,
            relation.ObjectType,
            relation.ObjectId,
            relation.Relation,
            relation.CreatedAtUtc);
}
