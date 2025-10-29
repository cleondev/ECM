using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.IAM.Application.Relations;
using ECM.IAM.Domain.Relations;

namespace ECM.IAM.Application.Relations.Commands;

public sealed class CreateAccessRelationCommandHandler(
    IAccessRelationRepository repository,
    ISystemClock clock)
{
    private readonly IAccessRelationRepository _repository = repository;
    private readonly ISystemClock _clock = clock;

    public async Task<OperationResult<AccessRelationSummaryResult>> HandleAsync(CreateAccessRelationCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedSubjectType = command.SubjectType?.Trim().ToLowerInvariant() ?? string.Empty;

        if (await _repository.GetAsync(
                normalizedSubjectType,
                command.SubjectId,
                command.ObjectType,
                command.ObjectId,
                command.Relation,
                cancellationToken) is not null)
        {
            return OperationResult<AccessRelationSummaryResult>.Failure("The relation already exists.");
        }

        AccessRelation relation;
        try
        {
            relation = AccessRelation.Create(
                command.SubjectType,
                command.SubjectId,
                command.ObjectType,
                command.ObjectId,
                command.Relation,
                _clock.UtcNow,
                command.ValidFromUtc,
                command.ValidToUtc);
        }
        catch (ArgumentException exception)
        {
            return OperationResult<AccessRelationSummaryResult>.Failure(exception.Message);
        }

        await _repository.AddAsync(relation, cancellationToken);

        return OperationResult<AccessRelationSummaryResult>.Success(relation.ToResult());
    }
}
