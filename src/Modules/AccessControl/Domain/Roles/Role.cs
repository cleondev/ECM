namespace ECM.AccessControl.Domain.Roles;

using ECM.AccessControl.Domain.Users;

public sealed class Role
{
    private readonly List<UserRole> _userRoles = [];

    private Role()
    {
        Name = null!;
    }

    private Role(Guid id, string name)
        : this()
    {
        Id = id;
        Name = name;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    public static Role Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Role name is required.", nameof(name));
        }

        return new Role(Guid.NewGuid(), name.Trim());
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Role name is required.", nameof(name));
        }

        Name = name.Trim();
    }
}
