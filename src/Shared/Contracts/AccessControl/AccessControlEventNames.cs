namespace Shared.Contracts.AccessControl;

public static class AccessControlEventNames
{
    public const string UserCreated = "ecm.access-control.user.created";
    public const string UserRoleAssigned = "ecm.access-control.user.role.assigned";
    public const string UserRoleRemoved = "ecm.access-control.user.role.removed";
    public const string AccessRelationCreated = "ecm.access-control.relation.created";
    public const string AccessRelationDeleted = "ecm.access-control.relation.deleted";
}
