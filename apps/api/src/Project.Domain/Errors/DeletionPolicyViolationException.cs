namespace Project.Domain.Errors;

public sealed class DeletionPolicyViolationException : DomainException
{
    public DeletionPolicyViolationException(string message) : base(message)
    {
    }
}
