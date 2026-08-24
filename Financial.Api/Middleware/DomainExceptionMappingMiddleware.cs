using Financial.CashFlow.Application.Exceptions;
using Financial.Investment.Application.Exceptions;
using Financial.Investment.Domain.Exceptions;
using Microsoft.AspNetCore.Http;

namespace Financial.Api.Middleware;

/// <summary>
/// Maps domain exceptions raised by Application services to the HTTP status code controllers
/// used to translate by hand, so no controller needs its own try/catch for these cases.
/// <para>
/// Both bounded contexts are mapped here. The BCL cases below already covered Investment, but a
/// condition Investment alone can raise needs its own entry or it falls through to a 500.
/// </para>
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
        catch (ReserveMovementLinkedToIncomeException ex)
        {
            await HandleAsync(context, ex, StatusCodes.Status409Conflict);
        }
        catch (UnsupportedAssetClassException ex)
        {
            // 422 rather than 400: the request is well formed and the asset is real, there is just
            // no price source for its class. 400 would invite the client to fix the request, and a
            // 500 - what this used to be - invites a retry that can never succeed.
            await HandleAsync(context, ex, StatusCodes.Status422UnprocessableEntity);
        }
        catch (KeyNotFoundException ex)
        {
            await HandleAsync(context, ex, StatusCodes.Status404NotFound);
        }
        catch (ArgumentException ex)
        {
            await HandleAsync(context, ex, StatusCodes.Status400BadRequest);
        }
        // A well-formed request the domain refuses on its own rules - moving an asset onto itself,
        // or into a portfolio that already holds one by that name. 409 rather than 400 because the
        // request is not malformed, and the distinction is what tells a client whether re-sending
        // the same body could ever succeed.
        //
        // Deliberately its own type rather than InvalidOperationException: Infrastructure already
        // throws that for genuine upstream faults (an unreadable Yahoo Finance response, a missing
        // price fetcher), and catching it here would relabel those as client conflicts and hide
        // real defects behind a 409.
        catch (InvestmentRuleViolationException ex)
        {
            await HandleAsync(context, ex, StatusCodes.Status409Conflict);
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
