using Project.Application.Common;

namespace Project.MigrationService;

/// <summary>
/// Validates superadmin credentials from environment/configuration before the seed
/// process begins. Rejects missing, whitespace-only, short, or malformed credentials
/// so the migration worker fails fast with a clear diagnostic.
/// </summary>
public static class SuperadminCredentialValidator
{
    /// <summary>
    /// Minimum superadmin password length enforced as defense in depth.
    /// </summary>
    public const int MinimumPasswordLength = 12;

    /// <summary>
    /// Validates the raw email and plaintext password. Returns a successful
    /// <see cref="Result{SuperadminCredentials}"/> only when both values are
    /// present, the email has a valid shape, and the password meets the minimum
    /// length requirement.
    /// </summary>
    public static Result<SuperadminCredentials> Validate(string? email, string? password)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return Result<SuperadminCredentials>.Failure(
                ErrorCodes.Superadmin.EmailMissing,
                "The SUPERADMIN_EMAIL environment variable is missing or empty. " +
                "Set it to a valid admin email address before starting the migration service.");
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            return Result<SuperadminCredentials>.Failure(
                ErrorCodes.Superadmin.PasswordMissing,
                "The SUPERADMIN_PASSWORD environment variable is missing or empty. " +
                "Set it to a strong password (minimum 12 characters) before starting the migration service.");
        }

        // ── Email shape validation (pragmatic pre-check before Domain Email.Create) ──

        var trimmedEmail = email.Trim().ToLowerInvariant();

        if (trimmedEmail.Any(char.IsWhiteSpace))
        {
            return Result<SuperadminCredentials>.Failure(
                ErrorCodes.Superadmin.EmailInvalid,
                $"The SUPERADMIN_EMAIL value '{email}' is not a valid email format. " +
                "Email values cannot contain whitespace characters.");
        }

        if (!trimmedEmail.Contains('@') || trimmedEmail.Count(c => c == '@') > 1)
        {
            return Result<SuperadminCredentials>.Failure(
                ErrorCodes.Superadmin.EmailInvalid,
                $"The SUPERADMIN_EMAIL value '{email}' is not a valid email format. " +
                "Provide a valid email address.");
        }

        var atIndex = trimmedEmail.IndexOf('@');
        if (atIndex == 0 || atIndex == trimmedEmail.Length - 1)
        {
            return Result<SuperadminCredentials>.Failure(
                ErrorCodes.Superadmin.EmailInvalid,
                $"The SUPERADMIN_EMAIL value '{email}' is not a valid email format. " +
                "Local and domain parts must be non-empty.");
        }

        var domainPart = trimmedEmail[(atIndex + 1)..];
        if (!domainPart.Contains('.'))
        {
            return Result<SuperadminCredentials>.Failure(
                ErrorCodes.Superadmin.EmailInvalid,
                $"The SUPERADMIN_EMAIL value '{email}' is not a valid email format. " +
                "The domain part must include at least one dot.");
        }

        // ── Password length check ──

        if (password.Length < MinimumPasswordLength)
        {
            return Result<SuperadminCredentials>.Failure(
                ErrorCodes.Superadmin.PasswordTooShort,
                $"The SUPERADMIN_PASSWORD must be at least {MinimumPasswordLength} characters long. " +
                $"The current value is only {password.Length} character(s).");
        }

        return Result<SuperadminCredentials>.Success(
            new SuperadminCredentials(trimmedEmail, password));
    }
}
