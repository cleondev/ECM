namespace Shared.Contracts.IAM;

public static class IamEventNames
{
    public const string UserCreated = "ecm.user.created";
    public const string UserRoleAssigned = "ecm.user-role.assigned";
    public const string UserRoleRemoved = "ecm.user-role.removed";
    public const string AccessRelationCreated = "ecm.access-relation.created";
    public const string AccessRelationDeleted = "ecm.access-relation.deleted";
}
