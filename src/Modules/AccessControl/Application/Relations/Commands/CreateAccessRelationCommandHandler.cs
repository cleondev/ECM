using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.AccessControl.Application.Relations;
using ECM.AccessControl.Domain.Relations;

namespace ECM.AccessControl.Application.Relations.Commands;

public sealed class CreateAccessRelationCommandHandler(
    IAccessRelationRepository repository,
    ISystemClock clock)
{
    private readonly IAccessRelationRepository _repository = repository;
    private readonly ISystemClock _clock = clock;

    public async Task<OperationResult<AccessRelationSummary>> HandleAsync(CreateAccessRelationCommand command, CancellationToken cancellationToken = default)
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

        return OperationResult<AccessRelationSummary>.Success(AccessRelationSummaryMapper.ToSummary(relation));
    }
}
