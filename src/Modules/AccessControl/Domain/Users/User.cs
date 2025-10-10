namespace ECM.Modules.AccessControl.Domain.Users;

using ECM.Modules.AccessControl.Domain.Roles;

public sealed class User
{
    private readonly List<UserRole> _roles = new();

    private User()
    {
        Email = null!;
        DisplayName = null!;
    }

    private User(
        Guid id,
        string email,
        string displayName,
        string? department,
        bool isActive,
        DateTimeOffset createdAtUtc)
        : this()
    {
        Id = id;
        Email = email;
        DisplayName = displayName;
        Department = department;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; }

    public string DisplayName { get; private set; }

    public string? Department { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    public static User Create(
        string email,
        string displayName,
        DateTimeOffset createdAtUtc,
        string? department = null,
        bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        return new User(
            Guid.NewGuid(),
            email.Trim(),
            displayName.Trim(),
            string.IsNullOrWhiteSpace(department) ? null : department.Trim(),
            isActive,
            createdAtUtc);
    }

    public void UpdateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("Display name is required.", nameof(displayName));
        }

        DisplayName = displayName.Trim();
    }

    public void UpdateDepartment(string? department)
    {
        Department = string.IsNullOrWhiteSpace(department) ? null : department.Trim();
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
