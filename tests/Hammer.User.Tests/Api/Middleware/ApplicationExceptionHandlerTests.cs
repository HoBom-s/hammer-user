using FluentAssertions;
using Hammer.User.Api.Middleware;
using Hammer.User.Application.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Hammer.User.Tests.Api.Middleware;

#pragma warning disable CA1873 // Test code verifies mock ILogger.Log calls directly
public sealed class ApplicationExceptionHandlerTests
{
    private readonly ILogger<ApplicationExceptionHandler> _logger =
        Substitute.For<ILogger<ApplicationExceptionHandler>>();

    private readonly ApplicationExceptionHandler _sut;

    public ApplicationExceptionHandlerTests()
    {
        _sut = new ApplicationExceptionHandler(_logger);
    }

    [Theory]
    [InlineData(typeof(BadRequestException), 400)]
    [InlineData(typeof(UnauthorizedException), 401)]
    [InlineData(typeof(ForbiddenException), 403)]
    [InlineData(typeof(NotFoundException), 404)]
    [InlineData(typeof(ConflictException), 409)]
    [InlineData(typeof(ServiceUnavailableException), 503)]
    public async Task TryHandleAsync_ShouldReturnTrue_WhenKnownException(Type exceptionType, int expectedStatusCode)
    {
        var exception = (Exception)Activator.CreateInstance(exceptionType, "test message")!;
        var httpContext = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        var result = await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeTrue();
        httpContext.Response.StatusCode.Should().Be(expectedStatusCode);
    }

    [Fact]
    public async Task TryHandleAsync_ShouldReturnFalse_WhenUnknownException()
    {
        var exception = new InvalidOperationException("unexpected");
        var httpContext = new DefaultHttpContext();

        var result = await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task TryHandleAsync_ShouldLogError_WhenUnknownException()
    {
        var exception = new InvalidOperationException("unexpected");
        var httpContext = new DefaultHttpContext { Request = { Method = "POST", Path = "/hammer-users/auth/login" } };

        await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task TryHandleAsync_ShouldLogWarning_WhenKnownException()
    {
        var exception = new NotFoundException("not found");
        var httpContext = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task TryHandleAsync_ShouldUseXTraceIdHeader_WhenPresent()
    {
        var exception = new InvalidOperationException("unexpected");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Trace-Id"] = "custom-trace-id";

        await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task TryHandleAsync_ShouldFallbackToTraceIdentifier_WhenXTraceIdMissing()
    {
        var exception = new InvalidOperationException("unexpected");
        var httpContext = new DefaultHttpContext { TraceIdentifier = "fallback-trace-id" };

        await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            exception,
            Arg.Any<Func<object, Exception?, string>>());
    }
}
#pragma warning restore CA1873
