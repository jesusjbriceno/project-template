using Microsoft.EntityFrameworkCore;
using Project.Application.Abstractions.Persistence;
using Project.Application.Abstractions.Security;
using Project.Domain.Common;
using Project.Domain.Entities;
using Project.Domain.ValueObjects;
using Project.Infrastructure.Data;

namespace Project.MigrationService;

/// <summary>
/// Static seed entry-point that bootstraps the RBAC catalog and initial
/// superadmin account. All entity creation goes through Domain factory methods;
/// persistence flows through the ApplicationDbContext change tracker inside a
/// single <c>SaveChangesAsync</c> call.
///
/// Idempotent: existing roles, permissions, and the superadmin user are detected
/// before insert and never mutated or duplicated.
/// </summary>
public static class SeedData
{
    public const string SystemActor = "SYSTEM";

    /// <summary>
    /// Applies the complete initial seed: system roles, permission catalog,
    /// superadmin role-permission assignments, and superadmin user with role.
    /// Safe to call on every deployment — second and subsequent invocations
    /// are no-ops.
    /// </summary>
    public static async Task SeedAsync(
        ApplicationDbContext dbContext,
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        string superadminEmail,
        string superadminPassword,
        IClock clock,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);
        ArgumentNullException.ThrowIfNull(roleRepository);
        ArgumentNullException.ThrowIfNull(permissionRepository);
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(superadminEmail);
        ArgumentNullException.ThrowIfNull(superadminPassword);
        ArgumentNullException.ThrowIfNull(clock);

        // ── 1. System roles (idempotent by name) ──

        var superadminRole = await GetOrCreateRoleAsync(
            dbContext, roleRepository, "Superadmin", isSystem: true, clock, ct);
        _ = await GetOrCreateRoleAsync(
            dbContext, roleRepository, "User", isSystem: true, clock, ct);

        if (!superadminRole.isNew)
        {
            await dbContext.Entry(superadminRole.entity)
                .Collection(r => r.RolePermissions)
                .LoadAsync(ct);
        }

        // ── 2. Permission catalog (idempotent by key) ──
        //
        // Fetch the existing permission set in a single bulk query and build an
        // in-memory dictionary for O(1) lookups, avoiding the N+1 roundtrip
        // pattern of querying per catalog entry.

        var existingPermissions = await permissionRepository.ListAsync(ct);
        var existingPermissionsByKey = existingPermissions.ToDictionary(p => p.Key);

        var permissionsByKey = new Dictionary<PermissionKey, Permission>();
        foreach (var entry in PermissionCatalog.All)
        {
            if (!existingPermissionsByKey.TryGetValue(entry.Key, out var permission))
            {
                permission = Permission.Create(
                    entry.Key, entry.Description, entry.Category, SystemActor, clock);
                dbContext.Permissions.Add(permission);
            }
            else
            {
                // Attach the pre-existing entity so subsequent role-permission
                // wiring works against the tracked instance.
                dbContext.Attach(permission);
            }

            permissionsByKey[entry.Key] = permission;
        }

        // ── 3. Assign missing catalog permissions to Superadmin role ──

        foreach (var entry in PermissionCatalog.All)
        {
            var permission = permissionsByKey[entry.Key];

            if (!superadminRole.entity.RolePermissions.Any(rp => rp.PermissionId == permission.Id))
            {
                superadminRole.entity.AddPermission(permission, SystemActor, clock);
            }
        }

        // ── 4. Superadmin user (idempotent by email) ──

        var emailVo = Email.Create(superadminEmail);
        var existingUser = await userRepository.GetByEmailAsync(emailVo, ct);

        if (existingUser is null)
        {
            var passwordHash = passwordHasher.Hash(superadminPassword);
            var user = User.Create(emailVo, passwordHash, SystemActor, clock);
            dbContext.Users.Add(user);

            // Bootstrap: provide the Superadmin role as actorRoles so the
            // Domain's AssignRole guard (only superadmins can create superadmins)
            // is satisfied without a live actor.
            user.AssignRole(superadminRole.entity, SystemActor, clock,
                actorRoles: new[] { superadminRole.entity });
        }

        // ── 5. Single atomic commit ──

        await dbContext.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Returns an existing role by name or creates and tracks a new one.
    /// The <c>isNew</c> flag on the returned tuple tells the caller whether
    /// the role was just created and therefore needs permission assignments.
    /// </summary>
    private static async Task<(Role entity, bool isNew)> GetOrCreateRoleAsync(
        ApplicationDbContext dbContext,
        IRoleRepository roleRepository,
        string name,
        bool isSystem,
        IClock clock,
        CancellationToken ct)
    {
        var existing = await roleRepository.GetByNameAsync(name, ct);

        if (existing is not null)
        {
            dbContext.Attach(existing);
            return (existing, false);
        }

        var role = Role.Create(name, isSystem, SystemActor, clock);
        dbContext.Roles.Add(role);
        return (role, true);
    }
}
