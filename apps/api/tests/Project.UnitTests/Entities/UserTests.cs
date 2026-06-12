using Project.Domain.Common;
using Project.Domain.Entities;
using Project.Domain.Errors;
using Project.Domain.Policies;
using Project.Domain.ValueObjects;
using Project.Domain.ValueObjects.Ids;

namespace Project.UnitTests.Entities;

public class UserTests
{
    private sealed class FakeClock : IClock
    {
        public DateTimeOffset UtcNow { get; init; } = new(2026, 6, 12, 10, 0, 0, TimeSpan.Zero);
    }

    // ── Task 4.1: User creation and lifecycle ──

    [Fact]
    public void Create_WithValidEmailAndPasswordHash_SetsProperties()
    {
        var clock = new FakeClock();
        var email = Email.Create("user@example.com");
        var passwordHash = "hashed-password-value";

        var user = User.Create(email, passwordHash, "system", clock);

        Assert.NotEqual(default, user.Id);
        Assert.Equal(email, user.Email);
        Assert.Equal(passwordHash, user.PasswordHash);
        Assert.True(user.IsActive);
        Assert.False(user.IsBlocked);
        Assert.False(string.IsNullOrWhiteSpace(user.SecurityStamp));
        Assert.Null(user.LastLoginAt);
        Assert.Empty(user.UserRoles);
        Assert.Equal(clock.UtcNow, user.CreatedAt);
        Assert.Equal("system", user.CreatedBy);
    }

    [Fact]
    public void Create_WithValidEmail_SetsIsActiveTrue()
    {
        var clock = new FakeClock();
        var email = Email.Create("newuser@example.com");

        var user = User.Create(email, "hash", "system", clock);

        Assert.True(user.IsActive);
    }

    [Fact]
    public void Create_GeneratesUniqueSecurityStamp()
    {
        var clock = new FakeClock();
        var email = Email.Create("a@example.com");

        var user1 = User.Create(email, "hash", "system", clock);
        var user2 = User.Create(email, "hash", "system", clock);

        Assert.NotEqual(user1.SecurityStamp, user2.SecurityStamp);
    }

    [Fact]
    public void Create_WithNullEmail_ThrowsArgumentNullException()
    {
        var clock = new FakeClock();

        var ex = Assert.Throws<ArgumentNullException>(() =>
            User.Create(null!, "hash", "system", clock));
        Assert.Contains("email", ex.ParamName!);
    }

    [Fact]
    public void Create_WithNullPasswordHash_ThrowsArgumentNullException()
    {
        var clock = new FakeClock();
        var email = Email.Create("user@example.com");

        var ex = Assert.Throws<ArgumentNullException>(() =>
            User.Create(email, null!, "system", clock));
        Assert.Contains("passwordHash", ex.ParamName!);
    }

    [Fact]
    public void Create_WithEmptyPasswordHash_ThrowsArgumentException()
    {
        var clock = new FakeClock();
        var email = Email.Create("user@example.com");

        var ex = Assert.Throws<ArgumentException>(() =>
            User.Create(email, "", "system", clock));
        Assert.Contains("passwordHash", ex.ParamName!);
    }

    [Fact]
    public void Deactivate_OnActiveUser_SetsIsActiveFalseAndRecordsUpdate()
    {
        var clock = new FakeClock();
        var later = new FakeClock { UtcNow = clock.UtcNow.AddHours(2) };
        var user = User.Create(Email.Create("user@example.com"), "hash", "system", clock);

        user.Deactivate("admin", later, Array.Empty<User>());

        Assert.False(user.IsActive);
        Assert.True(user.IsBlocked);
        Assert.Equal(later.UtcNow, user.UpdatedAt);
        Assert.Equal("admin", user.UpdatedBy);
    }

    [Fact]
    public void Deactivate_OnAlreadyInactiveUser_RemainsInactive()
    {
        var clock = new FakeClock();
        var later = new FakeClock { UtcNow = clock.UtcNow.AddHours(1) };
        var evenLater = new FakeClock { UtcNow = clock.UtcNow.AddHours(2) };
        var user = User.Create(Email.Create("user@example.com"), "hash", "system", clock);

        user.Deactivate("admin", later, Array.Empty<User>());
        user.Deactivate("admin", evenLater, Array.Empty<User>());

        Assert.False(user.IsActive);
    }

    [Fact]
    public void IsBlocked_WhenUserIsActive_ReturnsFalse()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("user@example.com"), "hash", "system", clock);

        Assert.False(user.IsBlocked);
    }

    [Fact]
    public void IsBlocked_WhenUserIsInactive_ReturnsTrue()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("user@example.com"), "hash", "system", clock);
        user.Deactivate("admin", clock, Array.Empty<User>());

        Assert.True(user.IsBlocked);
    }

    [Fact]
    public void DefaultPolicy_IsUserDefault()
    {
        Assert.Equal(DeletionPolicy.UserDefault, User.DefaultPolicy);
    }

    [Fact]
    public void User_ExtendsAuditableEntity()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("user@example.com"), "hash", "system", clock);

        Assert.True(user is AuditableEntity);
    }

    [Fact]
    public void Delete_NonSuperadmin_SucceedsAndBlocksAuth()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("user@example.com"), "hash", "system", clock);

        user.Delete(Array.Empty<User>(), "admin", clock);

        Assert.True(user.IsDeleted);
        Assert.False(user.IsActive);
        Assert.True(user.IsBlocked);
        Assert.Equal(clock.UtcNow, user.DeletedAt);
        Assert.Equal("admin", user.DeletedBy);
    }

    // ── Task 4.3: RemoveRole superadmin guard ──

    [Fact]
    public void AssignRole_WithValidRole_AddsToUserRoles()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("user@example.com"), "hash", "system", clock);
        var role = Role.Create("Editor", false, "system", clock);

        user.AssignRole(role, "admin", clock);

        Assert.Single(user.UserRoles);
        var ur = Assert.Single(user.UserRoles);
        Assert.Equal(user.Id, ur.UserId);
        Assert.Equal(role.Id, ur.RoleId);
        Assert.Equal("admin", ur.AssignedBy);
    }

    [Fact]
    public void AssignRole_DuplicateRole_ThrowsInvalidOperationException()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("user@example.com"), "hash", "system", clock);
        var role = Role.Create("Editor", false, "system", clock);

        user.AssignRole(role, "admin", clock);

        Assert.Throws<InvalidOperationException>(() =>
            user.AssignRole(role, "admin", clock));
    }

    [Fact]
    public void AssignRole_WithNullRole_ThrowsArgumentNullException()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("user@example.com"), "hash", "system", clock);

        var ex = Assert.Throws<ArgumentNullException>(() =>
            user.AssignRole(null!, "admin", clock));
        Assert.Contains("role", ex.ParamName!);
    }

    [Fact]
    public void RemoveRole_WithExistingRole_RemovesIt()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("user@example.com"), "hash", "system", clock);
        var role = Role.Create("Editor", false, "system", clock);
        user.AssignRole(role, "admin", clock);

        user.RemoveRole(role, activeSuperadmins: Array.Empty<User>(), clock);

        Assert.Empty(user.UserRoles);
    }

    [Fact]
    public void RemoveRole_WithNonExistingRole_DoesNothing()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("user@example.com"), "hash", "system", clock);
        var role = Role.Create("Editor", false, "system", clock);

        user.RemoveRole(role, activeSuperadmins: Array.Empty<User>(), clock);

        Assert.Empty(user.UserRoles);
    }

    [Fact]
    public void RemoveRole_WithNullRole_ThrowsArgumentNullException()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("user@example.com"), "hash", "system", clock);

        var ex = Assert.Throws<ArgumentNullException>(() =>
            user.RemoveRole(null!, activeSuperadmins: Array.Empty<User>(), clock));
        Assert.Contains("role", ex.ParamName!);
    }

    // ── Task 4.3: Last superadmin guard ──

    [Fact]
    public void RemoveRole_LastSuperadmin_ThrowsLastSuperadminGuardException()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("admin@example.com"), "hash", "system", clock);
        var superadminRole = Role.Create("superadmin", isSystem: true, "system", clock);
        user.AssignRole(superadminRole, "system", clock, [superadminRole]);

        // No other active superadmins → removal should be blocked
        var ex = Assert.Throws<LastSuperadminGuardException>(() =>
            user.RemoveRole(superadminRole, activeSuperadmins: [user], clock));

        Assert.Contains("superadmin", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void RemoveRole_LastSuperadmin_WhenOtherSuperadminExists_Succeeds()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("admin@example.com"), "hash", "system", clock);
        var otherAdmin = User.Create(Email.Create("other@example.com"), "hash", "system", clock);
        var superadminRole = Role.Create("superadmin", isSystem: true, "system", clock);
        user.AssignRole(superadminRole, "system", clock, [superadminRole]);
        otherAdmin.AssignRole(superadminRole, "system", clock, [superadminRole]);

        // Another active superadmin exists → removal succeeds
        user.RemoveRole(superadminRole, activeSuperadmins: [user, otherAdmin], clock);

        Assert.Empty(user.UserRoles);
    }

    [Fact]
    public void RemoveRole_NonSuperadminRole_DoesNotTriggerGuard()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("user@example.com"), "hash", "system", clock);
        var editorRole = Role.Create("Editor", isSystem: false, "system", clock);
        user.AssignRole(editorRole, "admin", clock);

        // Non-superadmin role removal should always succeed
        user.RemoveRole(editorRole, activeSuperadmins: Array.Empty<User>(), clock);

        Assert.Empty(user.UserRoles);
    }

    [Fact]
    public void RemoveRole_Superadmin_WhenInactiveUser_DoesNotTriggerGuard()
    {
        var clock = new FakeClock();
        var later = new FakeClock { UtcNow = clock.UtcNow.AddHours(1) };
        var user = User.Create(Email.Create("admin@example.com"), "hash", "system", clock);
        var superadminRole = Role.Create("superadmin", isSystem: true, "system", clock);
        user.AssignRole(superadminRole, "system", clock, [superadminRole]);
        user.Deactivate("admin", later, Array.Empty<User>());

        // User is inactive, removing superadmin role should succeed
        user.RemoveRole(superadminRole, activeSuperadmins: Array.Empty<User>(), later);

        Assert.Empty(user.UserRoles);
    }

    // ── Task 4.4: Superadmin creation gate ──

    [Fact]
    public void AssignRole_SuperadminRole_ByNonSuperadmin_ThrowsLastSuperadminGuardException()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("newadmin@example.com"), "hash", "system", clock);
        var superadminRole = Role.Create("superadmin", isSystem: true, "system", clock);
        var actorRoles = new[] { Role.Create("Editor", isSystem: false, "system", clock) };

        var ex = Assert.Throws<LastSuperadminGuardException>(() =>
            user.AssignRole(superadminRole, "user", clock, actorRoles));

        Assert.Contains("superadmin", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void AssignRole_SuperadminRole_BySuperadmin_Succeeds()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("newadmin@example.com"), "hash", "system", clock);
        var superadminRole = Role.Create("superadmin", isSystem: true, "system", clock);
        var actorRoles = new[] { superadminRole };

        user.AssignRole(superadminRole, "admin", clock, actorRoles);

        Assert.Single(user.UserRoles);
    }

    [Fact]
    public void AssignRole_NonSuperadminRole_ByNonSuperadmin_Succeeds()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("user@example.com"), "hash", "system", clock);
        var editorRole = Role.Create("Editor", isSystem: false, "system", clock);
        var actorRoles = Array.Empty<Role>();

        user.AssignRole(editorRole, "admin", clock, actorRoles);

        Assert.Single(user.UserRoles);
    }

    // ── Fix 1: Deactivate last-superadmin guard (regression) ──

    [Fact]
    public void Deactivate_LastActiveSuperadmin_ThrowsLastSuperadminGuardException()
    {
        var clock = new FakeClock();
        var later = new FakeClock { UtcNow = clock.UtcNow.AddHours(1) };
        var user = User.Create(Email.Create("admin@example.com"), "hash", "system", clock);
        var superadminRole = Role.Create("superadmin", isSystem: true, "system", clock);
        user.AssignRole(superadminRole, "system", clock, [superadminRole]);

        // User is the only active superadmin → deactivation blocked
        var ex = Assert.Throws<LastSuperadminGuardException>(() =>
            user.Deactivate("admin", later, [user]));

        Assert.Contains("superadmin", ex.Message.ToLowerInvariant());
        Assert.True(user.IsActive); // state unchanged
    }

    [Fact]
    public void Deactivate_Superadmin_WhenOtherActiveSuperadminExists_Succeeds()
    {
        var clock = new FakeClock();
        var later = new FakeClock { UtcNow = clock.UtcNow.AddHours(1) };
        var user = User.Create(Email.Create("admin1@example.com"), "hash", "system", clock);
        var otherAdmin = User.Create(Email.Create("admin2@example.com"), "hash", "system", clock);
        var superadminRole = Role.Create("superadmin", isSystem: true, "system", clock);
        user.AssignRole(superadminRole, "system", clock, [superadminRole]);
        otherAdmin.AssignRole(superadminRole, "system", clock, [superadminRole]);

        // Another active superadmin exists → deactivation succeeds
        user.Deactivate("admin", later, [user, otherAdmin]);

        Assert.False(user.IsActive);
    }

    [Fact]
    public void Deactivate_NonSuperadmin_WithSelfInSuperadmins_Succeeds()
    {
        var clock = new FakeClock();
        var later = new FakeClock { UtcNow = clock.UtcNow.AddHours(1) };
        var superadmin = User.Create(Email.Create("admin@example.com"), "hash", "system", clock);
        var regularUser = User.Create(Email.Create("user@example.com"), "hash", "system", clock);
        var superadminRole = Role.Create("superadmin", isSystem: true, "system", clock);
        superadmin.AssignRole(superadminRole, "system", clock, [superadminRole]);

        // Regular user is not a superadmin — deactivation always succeeds
        regularUser.Deactivate("admin", later, [superadmin]);

        Assert.False(regularUser.IsActive);
    }

    [Fact]
    public void Deactivate_AlreadyInactiveSuperadmin_Succeeds()
    {
        var clock = new FakeClock();
        var later = new FakeClock { UtcNow = clock.UtcNow.AddHours(1) };
        var evenLater = new FakeClock { UtcNow = clock.UtcNow.AddHours(2) };
        var user = User.Create(Email.Create("admin@example.com"), "hash", "system", clock);
        var superadminRole = Role.Create("superadmin", isSystem: true, "system", clock);
        user.AssignRole(superadminRole, "system", clock, [superadminRole]);

        // First deactivation with other superadmin present
        var otherAdmin = User.Create(Email.Create("other@example.com"), "hash", "system", clock);
        otherAdmin.AssignRole(superadminRole, "system", clock, [superadminRole]);
        user.Deactivate("admin", later, [user, otherAdmin]);

        Assert.False(user.IsActive);

        // Already inactive — guard uses IsActive check, so re-deactivation succeeds
        // even if this user is now the "last" superadmin in the list (inactive users
        // aren't blocked by the guard because IsActive is false)
        user.Deactivate("admin", evenLater, [user]);

        Assert.False(user.IsActive);
    }

    [Fact]
    public void Deactivate_WithNullActiveSuperadmins_ThrowsArgumentNullException()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("user@example.com"), "hash", "system", clock);

        var ex = Assert.Throws<ArgumentNullException>(() =>
            user.Deactivate("admin", clock, null!));
        Assert.Contains("activeSuperadmins", ex.ParamName!);
    }

    // ── Fix 2: MarkDeleted functional blocking (regression → updated) ──

    [Fact]
    public void Delete_AlreadyInactiveUser_Succeeds()
    {
        var clock = new FakeClock();
        var later = new FakeClock { UtcNow = clock.UtcNow.AddHours(1) };
        var user = User.Create(Email.Create("user@example.com"), "hash", "system", clock);
        user.Deactivate("admin", later, Array.Empty<User>());

        Assert.False(user.IsActive);

        user.Delete(Array.Empty<User>(), "admin", later);

        Assert.False(user.IsActive);
        Assert.True(user.IsDeleted);
    }

    [Fact]
    public void MarkDeleted_IsProtected_NotCallableExternally()
    {
        // MarkDeleted is now protected on AuditableEntity — external code cannot call it.
        // The only public deletion path is the entity-specific Delete() method.
        var auditableMethod = typeof(AuditableEntity).GetMethod(
            "MarkDeleted",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        Assert.Null(auditableMethod); // Not public

        var userMethod = typeof(User).GetMethod(
            "MarkDeleted",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        Assert.Null(userMethod); // Only Delete is the public API

        var roleMethod = typeof(Role).GetMethod(
            "MarkDeleted",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        Assert.Null(roleMethod); // Only Delete is the public API
    }

    // ── Fix 3: AssignRole superadmin gate bypass (regression) ──

    [Fact]
    public void AssignRole_SuperadminRole_WithNullActorRoles_ThrowsLastSuperadminGuardException()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("user@example.com"), "hash", "system", clock);
        var superadminRole = Role.Create("superadmin", isSystem: true, "system", clock);

        var ex = Assert.Throws<LastSuperadminGuardException>(() =>
            user.AssignRole(superadminRole, "admin", clock, null));

        Assert.Contains("actor role", ex.Message.ToLowerInvariant());
    }

    [Fact]
    public void AssignRole_SuperadminRole_WithEmptyActorRoles_ThrowsLastSuperadminGuardException()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("user@example.com"), "hash", "system", clock);
        var superadminRole = Role.Create("superadmin", isSystem: true, "system", clock);

        var ex = Assert.Throws<LastSuperadminGuardException>(() =>
            user.AssignRole(superadminRole, "admin", clock, Array.Empty<Role>()));

        Assert.Contains("actor role", ex.Message.ToLowerInvariant());
    }

    // ── Fix: MarkDeleted last-superadmin guard via Delete() ──

    [Fact]
    public void Delete_LastActiveSuperadmin_ThrowsLastSuperadminGuardException()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("admin@example.com"), "hash", "system", clock);
        var superadminRole = Role.Create("superadmin", isSystem: true, "system", clock);
        user.AssignRole(superadminRole, "system", clock, [superadminRole]);

        var ex = Assert.Throws<LastSuperadminGuardException>(() =>
            user.Delete([user], "admin", clock));

        Assert.Contains("superadmin", ex.Message.ToLowerInvariant());
        Assert.True(user.IsActive);    // state preserved — not deactivated
        Assert.False(user.IsDeleted);  // not soft-deleted
        Assert.False(user.IsBlocked);  // still has efective access
    }

    [Fact]
    public void Delete_Superadmin_WhenOtherActiveSuperadminExists_SucceedsAndBlocksAuth()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("admin1@example.com"), "hash", "system", clock);
        var otherAdmin = User.Create(Email.Create("admin2@example.com"), "hash", "system", clock);
        var superadminRole = Role.Create("superadmin", isSystem: true, "system", clock);
        user.AssignRole(superadminRole, "system", clock, [superadminRole]);
        otherAdmin.AssignRole(superadminRole, "system", clock, [superadminRole]);

        user.Delete([user, otherAdmin], "admin", clock);

        Assert.True(user.IsDeleted);
        Assert.False(user.IsActive);
        Assert.True(user.IsBlocked);
        Assert.Equal(clock.UtcNow, user.DeletedAt);
        Assert.Equal("admin", user.DeletedBy);
    }

    [Fact]
    public void Delete_WithNullActiveSuperadmins_ThrowsArgumentNullException()
    {
        var clock = new FakeClock();
        var user = User.Create(Email.Create("user@example.com"), "hash", "system", clock);

        var ex = Assert.Throws<ArgumentNullException>(() =>
            user.Delete(null!, "admin", clock));
        Assert.Contains("activeSuperadmins", ex.ParamName!);
    }
}
