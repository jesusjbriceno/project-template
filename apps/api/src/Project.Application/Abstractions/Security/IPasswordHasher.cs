namespace Project.Application.Abstractions.Security;

/// <summary>
/// Application-layer contract for password hashing and verification.
/// Decouples Domain/Application code from any specific cryptographic library.
/// Implementations MUST use a production-grade algorithm (BCrypt, Argon2id, or equivalent)
/// with a cost factor of at least 12 for BCrypt.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Produces a cryptographically strong hash of the given plaintext password.
    /// The returned string is self-contained (includes salt/algorithm metadata)
    /// and can be stored directly.
    /// </summary>
    string Hash(string plaintext);

    /// <summary>
    /// Verifies that a plaintext password matches a previously hashed value.
    /// Returns true when the password is correct; false otherwise.
    /// Implementations MUST use constant-time comparison or delegate verification
    /// to a password-hashing library that provides equivalent timing-attack resistance.
    /// </summary>
    bool Verify(string plaintext, string hash);
}
