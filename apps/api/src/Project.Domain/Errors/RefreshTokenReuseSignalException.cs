namespace Project.Domain.Errors;

public sealed class RefreshTokenReuseSignalException : DomainException
{
    public RefreshTokenReuseSignalException(string message) : base(message)
    {
    }
}
