namespace Project.Application.Abstractions.Messaging;

/// <summary>
/// Marker interface for commands. Commands represent write operations
/// that mutate state and return <see cref="Common.Result"/>.
/// Handled by <see cref="ICommandHandler{TCommand}"/>.
/// </summary>
public interface ICommand
{
}
