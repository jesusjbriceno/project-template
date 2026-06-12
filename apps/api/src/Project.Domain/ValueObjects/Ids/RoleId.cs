namespace Project.Domain.ValueObjects.Ids;

public sealed class RoleId : IEquatable<RoleId>
{
    public Guid Value { get; }

    private RoleId(Guid value) => Value = value;

    public static RoleId New() => new(Guid.NewGuid());

    public static RoleId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("RoleId cannot be empty.", nameof(value));
        return new(value);
    }

    public static implicit operator Guid(RoleId id) => id.Value;

    public bool Equals(RoleId? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is RoleId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();

    public static bool operator ==(RoleId? left, RoleId? right) => Equals(left, right);
    public static bool operator !=(RoleId? left, RoleId? right) => !Equals(left, right);
}
