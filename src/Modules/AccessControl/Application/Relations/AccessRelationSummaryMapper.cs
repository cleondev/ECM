using System;
using ECM.AccessControl.Domain.Relations;

namespace ECM.AccessControl.Application.Relations;

internal static class AccessRelationSummaryMapper
{
    public static AccessRelationSummary ToSummary(AccessRelation relation)
    {
        ArgumentNullException.ThrowIfNull(relation);

        return new AccessRelationSummary(
            relation.SubjectId,
            relation.ObjectType,
            relation.ObjectId,
            relation.Relation,
            relation.CreatedAtUtc);
    }
}
