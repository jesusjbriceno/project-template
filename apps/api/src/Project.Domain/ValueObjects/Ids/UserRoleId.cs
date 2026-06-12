namespace Project.Domain.ValueObjects.Ids;

public sealed class UserRoleId : IEquatable<UserRoleId>
{
    public UserId UserId { get; }
    public RoleId RoleId { get; }

    private UserRoleId(UserId userId, RoleId roleId)
    {
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        RoleId = roleId ?? throw new ArgumentNullException(nameof(roleId));
    }

    public static UserRoleId From(UserId userId, RoleId roleId) => new(userId, roleId);

    public void Deconstruct(out UserId userId, out RoleId roleId)
    {
        userId = UserId;
        roleId = RoleId;
    }

    public bool Equals(UserRoleId? other)
        => other is not null && UserId == other.UserId && RoleId == other.RoleId;
    public override bool Equals(object? obj) => obj is UserRoleId other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(UserId, RoleId);
    public override string ToString() => $"{UserId}_{RoleId}";

    public static bool operator ==(UserRoleId? left, UserRoleId? right) => Equals(left, right);
    public static bool operator !=(UserRoleId? left, UserRoleId? right) => !Equals(left, right);
}
