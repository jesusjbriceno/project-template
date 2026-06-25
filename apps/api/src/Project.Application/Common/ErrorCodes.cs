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

    /// <summary>
    /// Error codes used by auth handlers (login, refresh, logout).
    /// All prefixed <c>AUTH_</c>. Used in <see cref="Result.Failure"/>,
    /// never in Domain layer.
    /// </summary>
    public static class Auth
    {
        /// <summary>
        /// Unknown email, wrong password, or weak password — same generic code.
        /// </summary>
        public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";

        /// <summary>
        /// User exists but is deactivated (soft-deleted or directly blocked).
        /// </summary>
        public const string UserBlocked = "AUTH_USER_BLOCKED";

        /// <summary>
        /// Refresh token has expired. Client must re-authenticate.
        /// </summary>
        public const string TokenExpired = "AUTH_TOKEN_EXPIRED";

        /// <summary>
        /// Refresh token was already revoked (explicit logout or administrative action).
        /// </summary>
        public const string TokenRevoked = "AUTH_TOKEN_REVOKED";

        /// <summary>
        /// A previously-revoked token was presented again — potential token theft.
        /// The entire token family has been revoked as a security measure.
        /// </summary>
        public const string TokenReuseDetected = "AUTH_TOKEN_REUSE_DETECTED";

        /// <summary>
        /// No refresh token was provided (missing cookie or empty header).
        /// </summary>
        public const string RefreshTokenMissing = "AUTH_REFRESH_TOKEN_MISSING";
    }
}
