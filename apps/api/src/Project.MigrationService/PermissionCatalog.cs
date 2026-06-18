using Project.Domain.ValueObjects;

namespace Project.MigrationService;

/// <summary>
/// Static catalog of every permission known to the current system state.
/// Each entry maps 1:1 to a Domain operation; the Superadmin role receives all 22
/// during initial seed. Follow-up data migrations extend other roles as needed.
///
/// Keys follow the convention <c>resource.action</c> and must match the
/// <see cref="PermissionKey"/> regex (<c>[a-z][a-z0-9]*\.[a-z][a-z0-9]*</c>).
/// </summary>
public static class PermissionCatalog
{
    /// <summary>
    /// All 22 current-state permissions, grouped by resource.
    /// </summary>
    public static IReadOnlyList<CatalogEntry> All => _all;
    private static readonly CatalogEntry[] _all = BuildCatalog();

    /// <summary>
    /// Single entry: <see cref="PermissionKey"/>, human-readable
    /// <see cref="Description"/>, and optional <see cref="Category"/>.
    /// </summary>
    public sealed record CatalogEntry(PermissionKey Key, string Description, string? Category);

    private static CatalogEntry[] BuildCatalog()
    {
        return new[]
        {
            // ── users (7) ──
            Entry("users.read",         "View user list and details",            "users"),
            Entry("users.create",        "Create new users",                      "users"),
            Entry("users.update",        "Edit user profile and properties",      "users"),
            Entry("users.deactivate",    "Deactivate an active user",             "users"),
            Entry("users.delete",        "Soft-delete a user",                    "users"),
            Entry("users.assignrole",    "Assign a role to a user",               "users"),
            Entry("users.removerole",    "Remove a role from a user",             "users"),

            // ── roles (6) ──
            Entry("roles.read",              "View role list and details",         "roles"),
            Entry("roles.create",            "Create new roles",                   "roles"),
            Entry("roles.update",            "Edit role properties",               "roles"),
            Entry("roles.delete",            "Delete a non-system role",           "roles"),
            Entry("roles.assignpermission",  "Assign a permission to a role",      "roles"),
            Entry("roles.removepermission",  "Remove a permission from a role",    "roles"),

            // ── permissions (1) ──
            Entry("permissions.read", "View the permission catalog", "permissions"),

            // ── menu (5) ──
            Entry("menu.read",     "View menu items",                "menu"),
            Entry("menu.create",   "Create a new menu item",         "menu"),
            Entry("menu.update",   "Edit a menu item",               "menu"),
            Entry("menu.delete",   "Delete a menu item",             "menu"),
            Entry("menu.reorder",  "Reorder menu items",             "menu"),

            // ── auth (3) ──
            Entry("auth.refresh",       "Refresh an access token",          "auth"),
            Entry("auth.revoketoken",   "Revoke a single refresh token",    "auth"),
            Entry("auth.revokefamily",  "Revoke all tokens in a family",    "auth"),
        };
    }

    private static CatalogEntry Entry(string key, string description, string? category)
    {
        return new CatalogEntry(PermissionKey.Create(key), description, category);
    }
}
