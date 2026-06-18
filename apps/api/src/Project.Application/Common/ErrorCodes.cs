namespace Project.Application.Common;

/// <summary>
/// Stable, machine-readable error codes returned through <see cref="Error.Code"/>.
/// Defined as string constants to keep the public wire/serialization shape
/// unchanged (codes are strings across API, logs, and tests), while removing
/// the risk of typos at call sites that used inline string literals.
///
/// Convention: <c>DOMAIN_ENTITY_REASON</c>, upper snake-case.
/// </summary>
public static class ErrorCodes
{
    /// <summary>
    /// Error codes raised by the migration/seed bootstrap path for
    /// superadmin configuration validation.
    /// </summary>
    public static class Superadmin
    {
        /// <summary>
        /// The <c>SUPERADMIN_EMAIL</c> configuration value is missing, empty, or whitespace.
        /// </summary>
        public const string EmailMissing = "SUPERADMIN_EMAIL_MISSING";

        /// <summary>
        /// The <c>SUPERADMIN_PASSWORD</c> configuration value is missing, empty, or whitespace.
        /// </summary>
        public const string PasswordMissing = "SUPERADMIN_PASSWORD_MISSING";

        /// <summary>
        /// The <c>SUPERADMIN_EMAIL</c> configuration value is present but has an
        /// invalid email shape (missing <c>@</c>, missing local/domain parts,
        /// missing dot in domain, or contains internal whitespace).
        /// </summary>
        public const string EmailInvalid = "SUPERADMIN_EMAIL_INVALID";

        /// <summary>
        /// The <c>SUPERADMIN_PASSWORD</c> configuration value is shorter than the
        /// minimum length enforced by <c>SuperadminCredentialValidator.MinimumPasswordLength</c>.
        /// </summary>
        public const string PasswordTooShort = "SUPERADMIN_PASSWORD_TOO_SHORT";
    }
}
