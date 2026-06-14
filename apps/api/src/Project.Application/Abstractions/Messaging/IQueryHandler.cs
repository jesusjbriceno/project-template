namespace Project.Application.Abstractions.Messaging;

/// <summary>
/// Handles a query of type <typeparamref name="TQuery"/> and returns
/// data of type <typeparamref name="TResponse"/> wrapped in a <see cref="Common.Result{T}"/>.
/// </summary>
/// <typeparam name="TQuery">The query type. Must implement <see cref="IQuery{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of data returned by the query.</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Executes the query and returns the result containing the response data.
    /// </summary>
    /// <param name="query">The query to execute.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task representing the asynchronous operation, containing the result with data.</returns>
    Task<Common.Result<TResponse>> Handle(TQuery query, CancellationToken ct);
}
