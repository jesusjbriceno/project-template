namespace Project.MigrationService;

/// <summary>
/// Holds the raw superadmin email and plaintext password read from configuration
/// before validation and hashing. This type must never be logged or serialized
/// — it exists only in-memory during the seed bootstrap window.
/// </summary>
public sealed record SuperadminCredentials(string Email, string PlaintextPassword);
