namespace Project.Domain.ValueObjects.Ids;

public sealed class UserId : IEquatable<UserId>
{
    public Guid Value { get; }

    private UserId(Guid value) => Value = value;

    public static UserId New() => new(Guid.NewGuid());

    public static UserId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("UserId cannot be empty.", nameof(value));
        return new(value);
    }

    public static implicit operator Guid(UserId id) => id.Value;

    public bool Equals(UserId? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is UserId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();

    public static bool operator ==(UserId? left, UserId? right) => Equals(left, right);
    public static bool operator !=(UserId? left, UserId? right) => !Equals(left, right);
}
