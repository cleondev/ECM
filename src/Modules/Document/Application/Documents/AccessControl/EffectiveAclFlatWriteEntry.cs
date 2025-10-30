using System;

namespace ECM.Document.Application.Documents.AccessControl;

public readonly record struct EffectiveAclFlatWriteEntry(
    Guid DocumentId,
    Guid UserId,
    DateTimeOffset? ValidToUtc,
    string Source,
    string IdempotencyKey)
{
    public static EffectiveAclFlatWriteEntry ForOwner(Guid documentId, Guid ownerId)
    {
        return new EffectiveAclFlatWriteEntry(
            documentId,
            ownerId,
            ValidToUtc: null,
            Source: EffectiveAclFlatSources.Owner,
            IdempotencyKey: EffectiveAclFlatIdempotencyKeys.Owner);
    }
}

public static class EffectiveAclFlatSources
{
    public const string Owner = "document.owner";
}

public static class EffectiveAclFlatIdempotencyKeys
{
    public const string Owner = "owner";
}
