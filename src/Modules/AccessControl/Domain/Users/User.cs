namespace ECM.AccessControl.Domain.Users;

using System.Linq;
using ECM.AccessControl.Domain.Roles;
using ECM.AccessControl.Domain.Users.Events;
using ECM.BuildingBlocks.Domain.Events;

public sealed class User : IHasDomainEvents
{
    private readonly List<UserRole> _roles = [];
    private readonly List<IDomainEvent> _domainEvents = [];

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

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public bool HasRole(Guid roleId) => _roles.Any(link => link.RoleId == roleId);

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

        var user = new User(
            Guid.NewGuid(),
            email.Trim(),
            displayName.Trim(),
            string.IsNullOrWhiteSpace(department) ? null : department.Trim(),
            isActive,
            createdAtUtc);

        user.Raise(new UserCreatedDomainEvent(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Department,
            user.IsActive,
            createdAtUtc));

        return user;
    }

    public void AssignRole(Role role, DateTimeOffset assignedAtUtc)
    {
        if (role is null)
        {
            throw new ArgumentNullException(nameof(role));
        }

        if (HasRole(role.Id))
        {
            return;
        }

        _roles.Add(UserRole.Create(Id, role.Id, role));
        Raise(new UserRoleAssignedDomainEvent(Id, role.Id, role.Name, assignedAtUtc));
    }

    public void RemoveRole(Guid roleId, DateTimeOffset removedAtUtc)
    {
        var link = _roles.FirstOrDefault(existing => existing.RoleId == roleId);
        if (link is null)
        {
            return;
        }

        _roles.Remove(link);
        Raise(new UserRoleRemovedDomainEvent(Id, roleId, removedAtUtc));
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

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
}
