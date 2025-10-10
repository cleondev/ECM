namespace ECM.Modules.AccessControl.Domain.Users;

using ECM.Modules.AccessControl.Domain.Roles;

public sealed class UserRole
{
    private UserRole()
    {
        User = null!;
        Role = null!;
    }

    private UserRole(Guid userId, Guid roleId)
        : this()
    {
        UserId = userId;
        RoleId = roleId;
    }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    public User User { get; private set; }

    public Role Role { get; private set; }

    public static UserRole Create(Guid userId, Guid roleId) => new(userId, roleId);
}
