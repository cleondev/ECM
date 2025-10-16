namespace Shared.Contracts.IAM;

public static class IamEventNames
{
    public const string UserCreated = "ecm.iam.user.created";
    public const string UserRoleAssigned = "ecm.iam.user.role.assigned";
    public const string UserRoleRemoved = "ecm.iam.user.role.removed";
    public const string AccessRelationCreated = "ecm.iam.relation.created";
    public const string AccessRelationDeleted = "ecm.iam.relation.deleted";
}
