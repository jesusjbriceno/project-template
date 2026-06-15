namespace Project.Application.Abstractions.Messaging;

/// <summary>
/// Handles a command of type <typeparamref name="TCommand"/>.
/// Commands return a <see cref="Common.Result"/> (no value) to signal
/// success or failure.
/// </summary>
/// <typeparam name="TCommand">The command type. Must implement <see cref="ICommand"/>.</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Executes the command and returns a result indicating success or failure.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    Task<Common.Result> Handle(TCommand command, CancellationToken ct);
}
