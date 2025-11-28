using System;

namespace ECM.Document.Application.UserContext;

public sealed record DocumentCommandContext(Guid? OwnerId, Guid? CreatedBy);

public sealed record DocumentUserContext(Guid OwnerId, Guid CreatedBy, string? DisplayName);

public interface IDocumentUserContextResolver
{
    DocumentUserContext Resolve(DocumentCommandContext commandContext);
}
