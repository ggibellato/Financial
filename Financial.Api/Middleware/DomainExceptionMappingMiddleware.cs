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

    public DomainExceptionMappingMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (OverdraftConfirmationRequiredException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status409Conflict, ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status404NotFound, ex.Message);
        }
        catch (ArgumentException ex)
        {
            await WriteProblemAsync(context, StatusCodes.Status400BadRequest, ex.Message);
        }
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
