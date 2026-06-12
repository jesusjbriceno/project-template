namespace Project.Domain.Errors;

public sealed class InvalidEmailException : DomainException
{
    public InvalidEmailException(string message) : base(message)
    {
    }
}
