using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Project.Api.Controllers.Contracts;
using Project.Api.Controllers.Controllers;
using Project.Application.Auth;
using Project.Application.Common;
using Project.Infrastructure.Security;

namespace Project.UnitTests.Controllers;

/// <summary>
/// Unit tests for <see cref="AuthController"/>.
/// Verifies request-aborted cancellation is propagated to validation.
/// </summary>
public sealed class AuthControllerTests
{
    [Fact]
    public async Task Login_PassesRequestAbortedToValidator()
    {
        using var cts = new CancellationTokenSource();
        var loginValidator = new RecordingValidator<LoginCommand>(new ValidationResult([new ValidationFailure("Email", "Invalid")]));

        var controller = CreateController(
            loginValidator,
            new SuccessValidator<RefreshTokenCommand>(),
            new SuccessValidator<LogoutCommand>());

        SetHttpContext(controller, cts.Token);

        await controller.Login(new LoginRequest("user@example.com", "Password123!"));

        Assert.Equal(cts.Token, loginValidator.LastCancellationToken);
    }

    [Fact]
    public async Task Refresh_PassesRequestAbortedToValidator()
    {
        using var cts = new CancellationTokenSource();
        var refreshValidator = new RecordingValidator<RefreshTokenCommand>(new ValidationResult([new ValidationFailure("RefreshTokenRaw", "Invalid")]));

        var controller = CreateController(
            new SuccessValidator<LoginCommand>(),
            refreshValidator,
            new SuccessValidator<LogoutCommand>());

        SetHttpContext(controller, cts.Token, cookieHeader: "refreshToken=test-token");

        await controller.Refresh();

        Assert.Equal(cts.Token, refreshValidator.LastCancellationToken);
    }

    [Fact]
    public async Task Logout_PassesRequestAbortedToValidator()
    {
        using var cts = new CancellationTokenSource();
        var logoutValidator = new RecordingValidator<LogoutCommand>(new ValidationResult([new ValidationFailure("RefreshTokenRaw", "Invalid")]));

        var controller = CreateController(
            new SuccessValidator<LoginCommand>(),
            new SuccessValidator<RefreshTokenCommand>(),
            logoutValidator);

        SetHttpContext(controller, cts.Token, cookieHeader: "refreshToken=test-token");

        await controller.Logout();

        Assert.Equal(cts.Token, logoutValidator.LastCancellationToken);
    }

    private static AuthController CreateController(
        IValidator<LoginCommand> loginValidator,
        IValidator<RefreshTokenCommand> refreshValidator,
        IValidator<LogoutCommand> logoutValidator)
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            RefreshTokenDays = 7,
            Issuer = "test",
            Audience = "test",
            Secret = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA="
        });

        return new AuthController(
            CreateUninitialized<LoginCommandHandler>(),
            CreateUninitialized<RefreshTokenCommandHandler>(),
            CreateUninitialized<LogoutCommandHandler>(),
            loginValidator,
            refreshValidator,
            logoutValidator,
            jwtOptions,
            new FakeWebHostEnvironment());
    }

    private static void SetHttpContext(AuthController controller, CancellationToken requestAborted, string? cookieHeader = null)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestAborted = requestAborted
        };

        if (!string.IsNullOrWhiteSpace(cookieHeader))
            httpContext.Request.Headers.Cookie = cookieHeader;

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private static T CreateUninitialized<T>() where T : class => (T)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(T));

    private sealed class RecordingValidator<T>(ValidationResult result) : IValidator<T>
    {
        public CancellationToken LastCancellationToken { get; private set; }

        public ValidationResult Validate(T instance) => result;

        public Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellation = default)
        {
            LastCancellationToken = cancellation;
            return Task.FromResult(result);
        }

        public ValidationResult Validate(IValidationContext context) => result;

        public Task<ValidationResult> ValidateAsync(IValidationContext context, CancellationToken cancellation = default)
        {
            LastCancellationToken = cancellation;
            return Task.FromResult(result);
        }

        public IValidatorDescriptor CreateDescriptor() => throw new NotSupportedException();

        public bool CanValidateInstancesOfType(Type type) => true;
    }

    private sealed class SuccessValidator<T> : IValidator<T>
    {
        public ValidationResult Validate(T instance) => new();

        public Task<ValidationResult> ValidateAsync(T instance, CancellationToken cancellation = default) =>
            Task.FromResult(new ValidationResult());

        public ValidationResult Validate(IValidationContext context) => new();

        public Task<ValidationResult> ValidateAsync(IValidationContext context, CancellationToken cancellation = default) =>
            Task.FromResult(new ValidationResult());

        public IValidatorDescriptor CreateDescriptor() => throw new NotSupportedException();

        public bool CanValidateInstancesOfType(Type type) => true;
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "Project.Api.Controllers.Tests";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = string.Empty;
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
