using System.Collections.ObjectModel;
using Project.Domain.Common;
using Project.Domain.Errors;
using Project.Domain.Policies;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;

namespace Project.Domain.Entities;

/// <summary>
/// Represents a system user with authentication, authorization, and lifecycle management.
/// Users can be deactivated (soft-deleted); soft-deleted users are blocked from auth/authorization.
/// Cross-aggregate invariants (superadmin guards) accept pre-loaded sibling state as parameters,
/// keeping the Domain persistence-free and unit-testable.
/// </summary>
public sealed class User : AuditableEntity
{
    private readonly List<UserRole> _userRoles = new();

    public UserId Id { get; private set; }
    public Email Email { get; private set; }
    public bool IsActive { get; private set; }
    public string PasswordHash { get; private set; }
    public string SecurityStamp { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    /// <summary>
    /// Deactivated users are blocked from authentication and authorization.
    /// </summary>
    public bool IsBlocked => !IsActive;

    public static DeletionPolicy DefaultPolicy => DeletionPolicy.UserDefault;

#pragma warning disable CS8618
    private User()
    {
        // Private parameterless constructor for EF Core materialization.
        // Properties are set via their private setters after construction.
    }
#pragma warning restore CS8618

    private User(UserId id, Email email, string passwordHash, string createdBy, IClock clock)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        IsActive = true;
        SecurityStamp = Guid.NewGuid().ToString("N");
        LastLoginAt = null;

        MarkCreated(createdBy, clock);
    }

    public static User Create(Email email, string passwordHash, string createdBy, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(passwordHash);
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Password hash cannot be empty or whitespace.", nameof(passwordHash));

        return new User(UserId.New(), email, passwordHash, createdBy, clock);
    }

    /// <summary>
    /// Deactivates the user. Deactivated users are blocked from authentication and authorization.
    /// The last active superadmin cannot be deactivated.
    /// </summary>
    /// <param name="activeSuperadmins">
    /// All currently active users who hold the superadmin role (pre-built by the Application layer).
    /// Used to enforce the "at least one active superadmin" invariant.
    /// </param>
    public void Deactivate(string deactivatedBy, IClock clock,
        IReadOnlyCollection<User> activeSuperadmins)
    {
        ArgumentNullException.ThrowIfNull(activeSuperadmins);

        if (IsActive)
        {
            var isOnlyActiveSuperadmin = activeSuperadmins.Any(sa => sa.Id == Id)
                && !activeSuperadmins.Any(sa => sa.Id != Id);

            if (isOnlyActiveSuperadmin)
                throw new LastSuperadminGuardException(
                    "Cannot deactivate the last active superadmin. " +
                    "At least one active superadmin must remain in the system.");
        }

        IsActive = false;
        MarkUpdated(deactivatedBy, clock);
    }

    /// <summary>
    /// Assigns a role to this user.
    /// If the role is superadmin, the caller (actor) must also hold the superadmin role —
    /// non-superadmins cannot create superadmins regardless of generic permissions.
    /// No bypass: null or empty actorRoles throws when assigning superadmin.
    /// Bootstrap/seed scenarios must provide the superadmin role explicitly through
    /// a dedicated seeding operation outside this normal method.
    /// </summary>
    /// <param name="role">The role to assign.</param>
    /// <param name="assignedBy">Who is performing the assignment.</param>
    /// <param name="clock">Clock for audit timestamp.</param>
    /// <param name="actorRoles">
    /// Roles held by the actor performing this assignment.
    /// When assigning a superadmin role, this must be non-null, non-empty, and contain superadmin.
    /// </param>
    public UserRole AssignRole(Role role, string assignedBy, IClock clock,
        IReadOnlyCollection<Role>? actorRoles = null)
    {
        ArgumentNullException.ThrowIfNull(role);

        // Guard: only superadmins can create/assign superadmin roles.
        // Null or empty actorRoles is NOT a bypass — it is a security gap.
        if (IsSuperadminRole(role))
        {
            if (actorRoles is null || actorRoles.Count == 0)
            {
                throw new LastSuperadminGuardException(
                    "Cannot assign superadmin role without actor role context. " +
                    "Actor roles must be provided and must include superadmin to assign this role.");
            }

            var actorHasSuperadmin = actorRoles.Any(r => IsSuperadminRole(r));
            if (!actorHasSuperadmin)
            {
                throw new LastSuperadminGuardException(
                    $"Only a superadmin can create or assign the superadmin role. " +
                    $"Cannot assign role '{role.Name}' to user '{Email}'.");
            }
        }

        if (_userRoles.Any(ur => ur.RoleId == role.Id))
            throw new InvalidOperationException(
                $"Role '{role.Name}' is already assigned to user '{Email}'.");

        var userRole = UserRole.Assign(Id, role.Id, assignedBy, clock);
        _userRoles.Add(userRole);
        return userRole;
    }

    /// <summary>
    /// Removes a role from this user.
    /// If the role is superadmin and this user is the last active superadmin,
    /// the operation is blocked to maintain the invariant.
    /// </summary>
    /// <param name="role">The role to remove.</param>
    /// <param name="activeSuperadmins">
    /// All currently active users who hold the superadmin role (pre-built by the Application layer).
    /// Domain uses this collection to enforce the "at least one active superadmin" invariant.
    /// </param>
    /// <param name="clock">Clock for audit timestamp.</param>
    public void RemoveRole(Role role, IReadOnlyCollection<User> activeSuperadmins, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(role);

        if (IsSuperadminRole(role) && IsActive)
        {
            // Active superadmins collection is pre-filtered by the Application layer.
            // Check if any other active superadmin exists besides this user.
            var otherActiveSuperadminExists = activeSuperadmins.Any(sa => sa.Id != Id);

            if (!otherActiveSuperadminExists)
            {
                throw new LastSuperadminGuardException(
                    "Cannot remove the last active superadmin role. " +
                    "At least one active superadmin must remain in the system.");
            }
        }

        _userRoles.RemoveAll(ur => ur.RoleId == role.Id);
    }

    private static bool IsSuperadminRole(Role role)
        => role.IsSystem && "superadmin".Equals(role.Name, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Overrides soft-delete to functionally block the user from authentication and authorization.
    /// Soft-deleting a user sets <see cref="IsActive"/> to false so that <see cref="IsBlocked"/>
    /// returns true. Protected — external callers must use <see cref="Delete"/> which enforces
    /// the last-active-superadmin guard.
    /// </summary>
    protected override void MarkDeleted(string deletedBy, IClock clock)
    {
        IsActive = false;
        base.MarkDeleted(deletedBy, clock);
    }

    /// <summary>
    /// Soft-deletes the user with the last-active-superadmin invariant enforced.
    /// The only active superadmin cannot be soft-deleted.
    /// </summary>
    /// <param name="activeSuperadmins">
    /// All currently active users who hold the superadmin role (pre-built by the Application layer).
    /// </param>
    /// <param name="deletedBy">Who is performing the deletion.</param>
    /// <param name="clock">Clock for audit timestamp.</param>
    /// <exception cref="LastSuperadminGuardException">
    /// Thrown when this user is the only active superadmin.
    /// </exception>
    public void Delete(IReadOnlyCollection<User> activeSuperadmins, string deletedBy, IClock clock)
    {
        ArgumentNullException.ThrowIfNull(activeSuperadmins);

        if (IsActive)
        {
            var isOnlyActiveSuperadmin = activeSuperadmins.Any(sa => sa.Id == Id)
                && !activeSuperadmins.Any(sa => sa.Id != Id);

            if (isOnlyActiveSuperadmin)
                throw new LastSuperadminGuardException(
                    "Cannot delete the last active superadmin. " +
                    "At least one active superadmin must remain in the system.");
        }

        MarkDeleted(deletedBy, clock);
    }
}
