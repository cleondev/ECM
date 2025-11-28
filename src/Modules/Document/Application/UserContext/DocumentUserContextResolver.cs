using System;
using System.ComponentModel.DataAnnotations;
using ECM.Abstractions.Security;

namespace ECM.Document.Application.UserContext;

public sealed class DocumentUserContextResolver(ICurrentUserProvider currentUserProvider) : IDocumentUserContextResolver
{
    private readonly ICurrentUserProvider _currentUserProvider = currentUserProvider;

    public DocumentUserContext Resolve(DocumentCommandContext commandContext)
    {
        ArgumentNullException.ThrowIfNull(commandContext);

        var claimsUserId = _currentUserProvider.TryGetUserId();

        var ownerId = commandContext.OwnerId ?? claimsUserId
            ?? throw new ValidationException("OwnerId is required for this operation.");

        var createdBy = commandContext.CreatedBy ?? claimsUserId
            ?? throw new ValidationException("CreatedBy is required for this operation.");

        return new DocumentUserContext(ownerId, createdBy, _currentUserProvider.TryGetDisplayName());
    }
}
