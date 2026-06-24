namespace Project.Application.Auth;

/// <summary>
/// Static password policy for Application-layer enforcement.
/// Validates strength requirements without disclosing which rule failed.
/// Aligned with <c>SuperadminCredentialValidator.MinimumPasswordLength = 12</c>.
/// </summary>
public static class PasswordPolicy
{
    /// <summary>
    /// Minimum password length required for all users.
    /// </summary>
    public const int MinimumLength = 12;

    /// <summary>
    /// Returns true when the password meets all strength requirements:
    /// at least <see cref="MinimumLength"/> characters, one uppercase,
    /// one lowercase, one digit, and one special character.
    /// </summary>
    public static bool IsStrongEnough(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinimumLength)
            return false;

        var hasUpper = false;
        var hasLower = false;
        var hasDigit = false;
        var hasSpecial = false;

        foreach (var c in password)
        {
            if (char.IsUpper(c)) hasUpper = true;
            else if (char.IsLower(c)) hasLower = true;
            else if (char.IsDigit(c)) hasDigit = true;
            else hasSpecial = true;
        }

        return hasUpper && hasLower && hasDigit && hasSpecial;
    }
}
