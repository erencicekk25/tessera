using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Tessera.AspNetCore;

/// <summary>
/// Last line of defence: converts unhandled exceptions into problem details without leaking internals.
/// Optimistic-concurrency conflicts are surfaced as 409 so clients can simply retry.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetails)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            _ when exception.GetType().Name == "DbUpdateConcurrencyException" =>
                (StatusCodes.Status409Conflict, "The resource was modified concurrently. Please retry."),
            UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            OperationCanceledException => (499, "Request cancelled"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        if (status >= 500)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("Request {Method} {Path} ended with {Status}: {Message}",
                httpContext.Request.Method, httpContext.Request.Path, status, exception.Message);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title }
        });
    }
}
