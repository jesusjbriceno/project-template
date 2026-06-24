using Project.Application.Abstractions.Messaging;
using Project.Application.Auth;
using Project.Application.Common;

namespace Project.ApplicationTests.Abstractions.Messaging;

/// <summary>
/// Contract tests for the typed command handler abstraction.
/// Ensures value-returning commands have a proper interface contract
/// distinct from <see cref="ICommandHandler{TCommand}"/> which returns
/// <see cref="Result"/> (no value).
/// </summary>
public sealed class CommandHandlerContractTests
{
    [Fact]
    public void LoginCommandHandler_ImplementsTypedCommandHandler()
    {
        // ASSERT — LoginCommandHandler returns Result<TokenPairDto> and
        // should implement ICommandHandler<LoginCommand, TokenPairDto>
        var handlerType = typeof(LoginCommandHandler);
        var typedInterface = typeof(ICommandHandler<,>).MakeGenericType(typeof(LoginCommand), typeof(TokenPairDto));

        Assert.True(typedInterface.IsAssignableFrom(handlerType),
            $"LoginCommandHandler must implement ICommandHandler<LoginCommand, TokenPairDto>");
    }

    [Fact]
    public void RefreshTokenCommandHandler_ImplementsTypedCommandHandler()
    {
        var handlerType = typeof(RefreshTokenCommandHandler);
        var typedInterface = typeof(ICommandHandler<,>).MakeGenericType(typeof(RefreshTokenCommand), typeof(TokenPairDto));

        Assert.True(typedInterface.IsAssignableFrom(handlerType),
            $"RefreshTokenCommandHandler must implement ICommandHandler<RefreshTokenCommand, TokenPairDto>");
    }

    [Fact]
    public void LogoutCommandHandler_ImplementsCommandHandler()
    {
        var handlerType = typeof(LogoutCommandHandler);
        var handlerInterface = typeof(ICommandHandler<LogoutCommand>);

        Assert.True(handlerInterface.IsAssignableFrom(handlerType),
            $"LogoutCommandHandler must implement ICommandHandler<LogoutCommand>");
    }
}
