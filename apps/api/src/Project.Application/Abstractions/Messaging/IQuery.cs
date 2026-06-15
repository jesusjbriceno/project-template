namespace Project.Application.Abstractions.Messaging;

/// <summary>
/// Marker interface for queries. Queries represent read operations
/// that return data of type <typeparamref name="TResponse"/>.
/// Handled by <see cref="IQueryHandler{TQuery,TResponse}"/>.
/// </summary>
/// <typeparam name="TResponse">The type of data returned by the query.</typeparam>
public interface IQuery<TResponse>
{
}
