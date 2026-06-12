namespace Project.Domain.Errors;

public sealed class MenuCycleDetectedException : DomainException
{
    public MenuCycleDetectedException(string message) : base(message)
    {
    }
}
