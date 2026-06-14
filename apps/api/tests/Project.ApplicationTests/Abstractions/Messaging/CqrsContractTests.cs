using Project.Application.Abstractions.Messaging;
using Project.Application.Common;

namespace Project.ApplicationTests.Abstractions.Messaging;

/// <summary>
/// Compile-time contract proof: verifies that custom CQRS interface signatures
/// are correctly typed and accept CancellationToken. No external mediator dependency.
/// </summary>
public sealed class CqrsContractTests
{
    /// <summary>
    /// Sample command to prove <see cref="ICommand"/> marker works.
    /// </summary>
    private sealed record CreateItemCommand(string Name) : ICommand;

    /// <summary>
    /// Sample query to prove <see cref="IQuery{T}"/> marker works.
    /// </summary>
    private sealed record GetItemQuery(Guid Id) : IQuery<ItemDto>;

    private sealed record ItemDto(string Name);

    /// <summary>
    /// Sample handler proving the <see cref="ICommandHandler{TCommand}"/> signature:
    /// returns <see cref="Task{Result}"/> and accepts <see cref="CancellationToken"/>.
    /// </summary>
    private sealed class CreateItemCommandHandler : ICommandHandler<CreateItemCommand>
    {
        // Minimal implementation — compile-time proof only.
        public Task<Result> Handle(CreateItemCommand command, CancellationToken ct)
            => Task.FromResult(Result.Success());
    }

    /// <summary>
    /// Sample handler proving the <see cref="IQueryHandler{TQuery,TResponse}"/> signature:
    /// returns <see cref="Task{Result{TResponse}}"/> and accepts <see cref="CancellationToken"/>.
    /// </summary>
    private sealed class GetItemQueryHandler : IQueryHandler<GetItemQuery, ItemDto>
    {
        public Task<Result<ItemDto>> Handle(GetItemQuery query, CancellationToken ct)
            => Task.FromResult(Result<ItemDto>.Success(new ItemDto("Test")));
    }

    [Fact]
    public async Task CommandHandler_ReturnsResult_AndAcceptsCancellationToken()
    {
        var handler = new CreateItemCommandHandler();
        var command = new CreateItemCommand("test");

        Result result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task QueryHandler_ReturnsResultOfT_AndAcceptsCancellationToken()
    {
        var handler = new GetItemQueryHandler();
        var query = new GetItemQuery(Guid.NewGuid());

        Result<ItemDto> result = await handler.Handle(query, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Test", result.Value!.Name);
    }

    [Fact]
    public void Command_ImplementsICommand()
    {
        var command = new CreateItemCommand("x");

        // Compile-time: CreateItemCommand must be assignable to ICommand
        ICommand marker = command;
        Assert.NotNull(marker);
    }

    [Fact]
    public void Query_ImplementsIQueryOfT()
    {
        var query = new GetItemQuery(Guid.NewGuid());

        // Compile-time: GetItemQuery must be assignable to IQuery<ItemDto>
        IQuery<ItemDto> marker = query;
        Assert.NotNull(marker);
    }
}
