using Project.Application.Abstractions.Security;

namespace Project.Infrastructure.Security;

/// <summary>
/// BCrypt-based password hasher using <c>BCrypt.Net-Next</c> with cost factor 12.
/// Implements <see cref="IPasswordHasher"/> for use across the application.
/// Registered as singleton in <see cref="Project.Infrastructure.DependencyInjection"/>.
/// </summary>
public sealed class BCryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    /// <inheritdoc />
    public string Hash(string plaintext)
    {
        ArgumentNullException.ThrowIfNull(plaintext);

        return BCrypt.Net.BCrypt.EnhancedHashPassword(plaintext, WorkFactor);
    }

    /// <inheritdoc />
    public bool Verify(string plaintext, string hash)
    {
        ArgumentNullException.ThrowIfNull(plaintext);
        ArgumentNullException.ThrowIfNull(hash);

        try
        {
            return BCrypt.Net.BCrypt.EnhancedVerify(plaintext, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            return false;
        }
    }
}
