namespace Project.Application.Abstractions.Security;

/// <summary>
/// Application-facing security boundary for token-family revocation.
/// Exposes ONLY family-level revocation — no raw token issuance, validation,
/// or plaintext token handling. The Infrastructure layer owns the concrete
/// JWT implementation and token hasher.
/// </summary>
/// <remarks>
/// <para>
/// When <see cref="Domain.Entities.RefreshToken.Rotate"/> throws
/// <see cref="Domain.Errors.RefreshTokenReuseSignalException"/>,
/// the Application layer MUST call <see cref="RevokeFamilyAsync"/>
/// to revoke every token in the affected family as a security measure.
/// </para>
/// <para>
/// This interface MUST NOT expose:
/// token issuance (<c>IssueAsync</c>), validation (<c>ValidateAsync</c>),
/// raw token access, or plaintext token handling.
/// </para>
/// </remarks>
public interface ITokenService
{
    /// <summary>
    /// Revokes every token in the given family.
    /// Called when token reuse is detected during rotation.
    /// </summary>
    /// <param name="familyId">The token family to revoke entirely.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RevokeFamilyAsync(Guid familyId, CancellationToken ct = default);
}
