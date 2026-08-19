using Financial.CashFlow.Application.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Financial.Api.Middleware;

/// <summary>
/// Maps domain exceptions raised by Application services to the HTTP status code controllers
/// used to translate by hand, so no controller needs its own try/catch for these cases.
/// </summary>
internal sealed class DomainExceptionMappingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<DomainExceptionMappingMiddleware> _logger;

    public DomainExceptionMappingMiddleware(RequestDelegate next, ILogger<DomainExceptionMappingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OverdraftConfirmationRequiredException ex)
        {
            await HandleAsync(context, ex, StatusCodes.Status409Conflict);
        }
        catch (KeyNotFoundException ex)
        {
            await HandleAsync(context, ex, StatusCodes.Status404NotFound);
        }
        catch (ArgumentException ex)
        {
            await HandleAsync(context, ex, StatusCodes.Status400BadRequest);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception, int statusCode)
    {
        // Log the exception *type* only, never its message. Domain exception messages embed
        // financial values and entity names (e.g. "exceeds Ariana's balance of 654.27"), which
        // must stay out of the log stream. The caller still gets the full message in the
        // response body - only the log is redacted.
        _logger.LogWarning(
            "Request {RequestMethod} {RequestPath} rejected with {StatusCode} by {ErrorType}",
            context.Request.Method,
            context.Request.Path,
            statusCode,
            exception.GetType().Name);

        await WriteProblemAsync(context, statusCode, exception.Message);
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string detail)
    {
        context.Response.StatusCode = statusCode;

        var problemDetailsService = context.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = { Status = statusCode, Detail = detail }
        });
    }
}
