using System.ComponentModel.DataAnnotations;

namespace Project.Infrastructure.Security;

/// <summary>
/// Configuration options for JWT token generation and validation.
/// Bound from the "Jwt" section in configuration at startup,
/// with validation enforced via <c>ValidateDataAnnotations().ValidateOnStart()</c>.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// HS256 signing key. Must be at least 32 bytes.
    /// </summary>
    [Required]
    [MinLength(32, ErrorMessage = "JWT Secret must be at least 32 characters.")]
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Token issuer (the application).
    /// </summary>
    [Required(ErrorMessage = "JWT Issuer is required.")]
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Token audience (the expected consumer).
    /// </summary>
    [Required(ErrorMessage = "JWT Audience is required.")]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token lifetime in days. Defaults to 7.
    /// </summary>
    [Range(1, 365, ErrorMessage = "Refresh token days must be between 1 and 365.")]
    public int RefreshTokenDays { get; set; } = 7;
}
