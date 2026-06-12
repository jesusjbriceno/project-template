namespace Project.Domain.Policies;

/// <summary>
/// Defines the deletion behavior for a domain entity.
/// All four dimensions are independent — they are not synonyms.
/// </summary>
/// <param name="SoftDeleteEnabled">Whether the entity supports logical deletion (audit marker).</param>
/// <param name="RecycleBinVisible">Whether soft-deleted records are visible in a recycle bin.</param>
/// <param name="RestoreAllowed">Whether soft-deleted records can be restored.</param>
/// <param name="HardDeleteAllowed">Whether physical/permanent deletion is allowed.</param>
public sealed record DeletionPolicy(
    bool SoftDeleteEnabled,
    bool RecycleBinVisible,
    bool RestoreAllowed,
    bool HardDeleteAllowed)
{
    /// <summary>
    /// User default: soft-delete only. No recycle bin, no restore, no hard delete.
    /// Soft-deleted users are blocked from auth/authorization.
    /// </summary>
    public static DeletionPolicy UserDefault => new(
        SoftDeleteEnabled: true,
        RecycleBinVisible: false,
        RestoreAllowed: false,
        HardDeleteAllowed: false);

    /// <summary>
    /// RefreshToken default: no recycle-bin semantics.
    /// Lifecycle is governed by revocation and expiration, not soft/hard delete.
    /// </summary>
    public static DeletionPolicy RefreshTokenDefault => new(
        SoftDeleteEnabled: false,
        RecycleBinVisible: false,
        RestoreAllowed: false,
        HardDeleteAllowed: false);

    /// <summary>
    /// Role default: soft-delete enabled, restore allowed (subject to consistency rules).
    /// System roles are additionally protected from deletion entirely.
    /// </summary>
    public static DeletionPolicy RoleDefault => new(
        SoftDeleteEnabled: true,
        RecycleBinVisible: true,
        RestoreAllowed: true,
        HardDeleteAllowed: false);

    /// <summary>
    /// MenuItem default: soft-delete enabled, restore allowed (subject to hierarchy consistency).
    /// </summary>
    public static DeletionPolicy MenuItemDefault => new(
        SoftDeleteEnabled: true,
        RecycleBinVisible: true,
        RestoreAllowed: true,
        HardDeleteAllowed: false);

    /// <summary>
    /// Permission default: permissions are catalog entries.
    /// Hard delete only — no soft delete semantics needed.
    /// </summary>
    public static DeletionPolicy PermissionDefault => new(
        SoftDeleteEnabled: false,
        RecycleBinVisible: false,
        RestoreAllowed: false,
        HardDeleteAllowed: true);
}
