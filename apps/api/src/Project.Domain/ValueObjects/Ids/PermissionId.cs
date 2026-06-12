namespace Project.Domain.ValueObjects.Ids;

public sealed class PermissionId : IEquatable<PermissionId>
{
    public Guid Value { get; }

    private PermissionId(Guid value) => Value = value;

    public static PermissionId New() => new(Guid.NewGuid());

    public static PermissionId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("PermissionId cannot be empty.", nameof(value));
        return new(value);
    }

    public static implicit operator Guid(PermissionId id) => id.Value;

    public bool Equals(PermissionId? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is PermissionId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();

    public static bool operator ==(PermissionId? left, PermissionId? right) => Equals(left, right);
    public static bool operator !=(PermissionId? left, PermissionId? right) => !Equals(left, right);
}
