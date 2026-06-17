using Project.Application.Abstractions.Security;
using Project.Domain.Entities;
using Project.Domain.ValueObjects.Ids;

namespace Project.Infrastructure.Security;

/// <summary>
/// Scaffold implementation of IUserSession for the initial Infrastructure setup.
/// Returns null/no-session values — the real implementation will be wired
/// once the security/JWT slice is built (ef-core-security change).
/// </summary>
public sealed class NullUserSession : IUserSession
{
    public UserId? UserId => null;
    public string? Email => null;
    public IReadOnlyCollection<Role> Roles => Array.Empty<Role>();
    public bool IsAuthenticated => false;
}
