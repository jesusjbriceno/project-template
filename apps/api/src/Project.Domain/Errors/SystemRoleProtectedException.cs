namespace Project.Domain.Errors;

public sealed class SystemRoleProtectedException : DomainException
{
    public SystemRoleProtectedException(string message) : base(message)
    {
    }
}
