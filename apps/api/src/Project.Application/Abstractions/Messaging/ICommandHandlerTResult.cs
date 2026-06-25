namespace Project.Application.Abstractions.Messaging;

/// <summary>
/// Handles a command of type <typeparamref name="TCommand"/> that produces
/// a value of type <typeparamref name="TResult"/>.
/// Distinct from <see cref="ICommandHandler{TCommand}"/> which returns
/// <see cref="Common.Result"/> (no value).
/// </summary>
/// <typeparam name="TCommand">The command type. Must implement <see cref="ICommand"/>.</typeparam>
/// <typeparam name="TResult">The type of the value produced on success.</typeparam>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand
{
    /// <summary>
    /// Executes the command and returns a result containing the produced value.
    /// </summary>
    /// <param name="command">The command to execute.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, containing the typed result.</returns>
    Task<Common.Result<TResult>> Handle(TCommand command, CancellationToken ct);
}
