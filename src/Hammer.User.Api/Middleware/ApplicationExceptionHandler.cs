using Hammer.User.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Serilog.Context;

namespace Hammer.User.Api.Middleware;

/// <summary>
///     Maps application exceptions to HTTP problem details responses.
/// </summary>
internal sealed class ApplicationExceptionHandler(
    ILogger<ApplicationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        var (statusCode, title) = exception switch
        {
            BadRequestException => (StatusCodes.Status400BadRequest, "Bad Request"),
            UnauthorizedException or SecurityTokenException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            ServiceUnavailableException => (StatusCodes.Status503ServiceUnavailable, "Service Unavailable"),
            _ => (0, string.Empty),
        };

        var traceId = httpContext.Request.Headers["X-Trace-Id"].FirstOrDefault()
            ?? httpContext.TraceIdentifier;

        if (statusCode == 0)
        {
            using (LogContext.PushProperty("TraceId", traceId))
            using (LogContext.PushProperty("RequestPath", httpContext.Request.Path.Value))
            using (LogContext.PushProperty("RequestMethod", httpContext.Request.Method))
                logger.LogError(exception, "Unhandled exception");

            return false;
        }

        using (LogContext.PushProperty("TraceId", traceId))
        using (LogContext.PushProperty("RequestPath", httpContext.Request.Path.Value))
        using (LogContext.PushProperty("RequestMethod", httpContext.Request.Method))
        using (LogContext.PushProperty("StatusCode", statusCode))
            logger.LogWarning(exception, "Application exception");

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = statusCode, Title = title, Detail = exception.Message },
            cancellationToken);

        return true;
    }
}
