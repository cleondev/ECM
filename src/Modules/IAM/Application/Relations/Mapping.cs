using System;
using ECM.IAM.Domain.Relations;

namespace ECM.IAM.Application.Relations;

internal static class Mapping
{
    public static AccessRelationSummaryResult ToResult(this AccessRelation relation)
    {
        ArgumentNullException.ThrowIfNull(relation);

        return new AccessRelationSummaryResult(
            relation.SubjectType,
            relation.SubjectId,
            relation.ObjectType,
            relation.ObjectId,
            relation.Relation,
            relation.CreatedAtUtc,
            relation.ValidFromUtc,
            relation.ValidToUtc);
    }
}
