using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.BuildingBlocks.Application;
using ECM.BuildingBlocks.Application.Abstractions.Time;
using ECM.Document.Application.Tags.Repositories;

namespace ECM.Document.Application.Tags.Commands;

public sealed class DeleteTagLabelCommandHandler(
    ITagLabelRepository tagLabelRepository,
    ISystemClock clock)
{
    private readonly ITagLabelRepository _tagLabelRepository = tagLabelRepository;
    private readonly ISystemClock _clock = clock;

    public async Task<OperationResult<bool>> HandleAsync(DeleteTagLabelCommand command, CancellationToken cancellationToken = default)
    {
        if (command.TagId == Guid.Empty)
        {
            return OperationResult<bool>.Failure("Tag identifier is required.");
        }

        var tagLabel = await _tagLabelRepository.GetByIdAsync(command.TagId, cancellationToken).ConfigureAwait(false);
        if (tagLabel is null)
        {
            return OperationResult<bool>.Failure("Tag label was not found.");
        }

        tagLabel.MarkDeleted(_clock.UtcNow);
        await _tagLabelRepository.RemoveAsync(tagLabel, cancellationToken).ConfigureAwait(false);
        return OperationResult<bool>.Success(true);
    }
}
