namespace Project.Domain.ValueObjects.Ids;

public sealed class RolePermissionId : IEquatable<RolePermissionId>
{
    public RoleId RoleId { get; }
    public PermissionId PermissionId { get; }

    private RolePermissionId(RoleId roleId, PermissionId permissionId)
    {
        RoleId = roleId ?? throw new ArgumentNullException(nameof(roleId));
        PermissionId = permissionId ?? throw new ArgumentNullException(nameof(permissionId));
    }

    public static RolePermissionId From(RoleId roleId, PermissionId permissionId) => new(roleId, permissionId);

    public void Deconstruct(out RoleId roleId, out PermissionId permissionId)
    {
        roleId = RoleId;
        permissionId = PermissionId;
    }

    public bool Equals(RolePermissionId? other)
        => other is not null && RoleId == other.RoleId && PermissionId == other.PermissionId;
    public override bool Equals(object? obj) => obj is RolePermissionId other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(RoleId, PermissionId);
    public override string ToString() => $"{RoleId}_{PermissionId}";

    public static bool operator ==(RolePermissionId? left, RolePermissionId? right) => Equals(left, right);
    public static bool operator !=(RolePermissionId? left, RolePermissionId? right) => !Equals(left, right);
}
