using System.Text.RegularExpressions;
using Project.Domain.Errors;

namespace Project.Domain.ValueObjects;

public sealed partial record PermissionKey
{
    /// <summary>
    /// Pattern: exactly two lowercase dot-separated segments (action.resource).
    /// Each segment starts with a letter, followed by letters/digits.
    /// No leading/trailing dots, no consecutive dots.
    /// </summary>
    [GeneratedRegex(@"^[a-z][a-z0-9]*\.[a-z][a-z0-9]*$", RegexOptions.Compiled)]
    private static partial Regex KeyPattern();

    public string Value { get; }

    private PermissionKey(string value) => Value = value;

    public static PermissionKey Create(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (string.IsNullOrWhiteSpace(value) || !KeyPattern().IsMatch(value))
            throw new InvalidPermissionKeyException($"'{value}' is not a valid permission key.");

        return new PermissionKey(value);
    }

    public override string ToString() => Value;
}
