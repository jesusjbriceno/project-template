using Project.Domain.Errors;

namespace Project.Domain.ValueObjects;

public sealed record Email
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var trimmed = value.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
            throw new InvalidEmailException($"'{value}' is not a valid email address.");

        // No whitespace characters anywhere in the email
        if (trimmed.Any(char.IsWhiteSpace))
            throw new InvalidEmailException($"'{value}' is not a valid email address.");

        if (!trimmed.Contains('@'))
            throw new InvalidEmailException($"'{value}' is not a valid email address.");

        if (trimmed.Count(c => c == '@') > 1)
            throw new InvalidEmailException($"'{value}' is not a valid email address.");

        var atIndex = trimmed.IndexOf('@');
        var localPart = trimmed[..atIndex];
        var domainPart = trimmed[(atIndex + 1)..];

        if (localPart.Length == 0 || domainPart.Length == 0)
            throw new InvalidEmailException($"'{value}' is not a valid email address.");

        // Pragmatic: require at least one dot in the domain part (TLD)
        if (!domainPart.Contains('.'))
            throw new InvalidEmailException($"'{value}' is not a valid email address.");

        // Normalize: lowercase the entire email per spec
        var normalized = trimmed.ToLowerInvariant();

        return new Email(normalized);
    }

    public override string ToString() => Value;
}
