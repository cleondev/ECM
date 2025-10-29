namespace ECM.IAM.Domain.Users;

using System.Collections.Generic;
using System.Linq;
using ECM.IAM.Domain.Groups;
using ECM.IAM.Domain.Roles;
using ECM.IAM.Domain.Users.Events;
using ECM.BuildingBlocks.Domain.Events;

public sealed class User : IHasDomainEvents
{
    private readonly List<UserRole> _roles = [];
    private readonly List<IDomainEvent> _domainEvents = [];
    private readonly List<GroupMember> _groups = [];

    private User()
    {
        Email = null!;
        DisplayName = null!;
    }

    private User(
        Guid id,
        string email,
        string displayName,
        bool isActive,
        DateTimeOffset createdAtUtc,
        string? passwordHash)
        : this()
    {
        Id = id;
        Email = email;
        DisplayName = displayName;
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
        PasswordHash = passwordHash;
    }

    public Guid Id { get; private set; }

    public string Email { get; private set; }

    public string DisplayName { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string? PasswordHash { get; private set; }

    public IReadOnlyCollection<UserRole> Roles => _roles.AsReadOnly();

    public IReadOnlyCollection<GroupMember> Groups => _groups.AsReadOnly();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public bool HasRole(Guid roleId) => _roles.Any(link => link.RoleId == roleId);

    public static User Create(
        string email,
        string displayName,
        DateTimeOffset createdAtUtc,
        bool isActive = true,
        string? passwordHash = null)
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
            isActive,
            createdAtUtc,
            string.IsNullOrWhiteSpace(passwordHash) ? null : passwordHash);

        user.Raise(new UserCreatedDomainEvent(
            user.Id,
            user.Email,
            user.DisplayName,
            user.IsActive,
            createdAtUtc));

        return user;
    }

    public void AssignRole(Role role, DateTimeOffset assignedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(role);

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

    public void SetPasswordHash(string? passwordHash)
    {
        PasswordHash = string.IsNullOrWhiteSpace(passwordHash) ? null : passwordHash;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public void ClearDomainEvents() => _domainEvents.Clear();

    internal void SyncGroups(IEnumerable<GroupMember> memberships)
    {
        ArgumentNullException.ThrowIfNull(memberships);

        _groups.Clear();
        _groups.AddRange(memberships);
    }

    private void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
}
