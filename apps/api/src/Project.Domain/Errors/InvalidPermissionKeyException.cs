namespace Project.Domain.Errors;

public sealed class InvalidPermissionKeyException : DomainException
{
    public InvalidPermissionKeyException(string message) : base(message)
    {
    }
}
