namespace Project.Domain.ValueObjects.Ids;

public sealed class RefreshTokenId : IEquatable<RefreshTokenId>
{
    public Guid Value { get; }

    private RefreshTokenId(Guid value) => Value = value;

    public static RefreshTokenId New() => new(Guid.NewGuid());

    public static RefreshTokenId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("RefreshTokenId cannot be empty.", nameof(value));
        return new(value);
    }

    public static implicit operator Guid(RefreshTokenId id) => id.Value;

    public bool Equals(RefreshTokenId? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is RefreshTokenId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();

    public static bool operator ==(RefreshTokenId? left, RefreshTokenId? right) => Equals(left, right);
    public static bool operator !=(RefreshTokenId? left, RefreshTokenId? right) => !Equals(left, right);
}
