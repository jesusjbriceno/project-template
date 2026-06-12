namespace Project.Domain.Errors;

public sealed class LastSuperadminGuardException : DomainException
{
    public LastSuperadminGuardException(string message) : base(message)
    {
    }
}
