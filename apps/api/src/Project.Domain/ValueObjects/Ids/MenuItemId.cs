namespace Project.Domain.ValueObjects.Ids;

public sealed class MenuItemId : IEquatable<MenuItemId>
{
    public Guid Value { get; }

    private MenuItemId(Guid value) => Value = value;

    public static MenuItemId New() => new(Guid.NewGuid());

    public static MenuItemId From(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("MenuItemId cannot be empty.", nameof(value));
        return new(value);
    }

    public static implicit operator Guid(MenuItemId id) => id.Value;

    public bool Equals(MenuItemId? other) => other is not null && Value == other.Value;
    public override bool Equals(object? obj) => obj is MenuItemId other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();

    public static bool operator ==(MenuItemId? left, MenuItemId? right) => Equals(left, right);
    public static bool operator !=(MenuItemId? left, MenuItemId? right) => !Equals(left, right);
}
