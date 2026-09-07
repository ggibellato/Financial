using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages the optional calendar connection used to sync credit-card due dates. Depends only on
/// <see cref="ICalendarIntegrationService"/> - Google is the only concrete provider wired in
/// today (see <c>GoogleCalendarProviderAdapter</c>), but this controller has no Google-specific
/// code. The "Google" in this class's name and route reflects that composition-root choice.
/// </summary>
[ApiController]
[Route("integrations/google-calendar")]
public sealed class GoogleCalendarIntegrationController : ControllerBase
{
    private readonly ICalendarIntegrationService _service;

    public GoogleCalendarIntegrationController(ICalendarIntegrationService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>Returns the current connection status.</summary>
    /// <returns>200 OK with the connection status.</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(CalendarConnectionStatusDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<CalendarConnectionStatusDTO>> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _service.GetStatusAsync(cancellationToken);
        return Ok(status);
    }

    /// <summary>Redirects the browser to the provider's consent screen.</summary>
    /// <returns>302 redirect to the OAuth consent URL.</returns>
    [HttpGet("connect")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult Connect()
    {
        var url = _service.BuildAuthorizationUrl();
        return Redirect(url);
    }

    /// <summary>The OAuth redirect target. Completes the connection and renders a landing
    /// page telling the user to return to the app - never called by either front end's API client.</summary>
    /// <returns>200 OK with a minimal HTML success/failure page.</returns>
    [HttpGet("callback")]
    [Produces("text/html")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ContentResult> Callback(
        [FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, CancellationToken cancellationToken)
    {
        var result = await _service.CompleteConnectionAsync(code, state, error, cancellationToken);
        var html = result.Success
            ? BuildLandingPage("Connected!", "You can close this tab and return to the app.")
            : BuildLandingPage("Connection not completed", result.ErrorMessage ?? "Please try again.");

        return Content(html, "text/html");
    }

    /// <summary>Removes the calendar connection - deletes the dedicated calendar, revokes
    /// the token, and clears local state, always clearing local state even if the remote calls fail.</summary>
    /// <returns>200 OK with whether the remote cleanup succeeded.</returns>
    [HttpPost("disconnect")]
    [ProducesResponseType(typeof(CalendarDisconnectResultDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<CalendarDisconnectResultDTO>> Disconnect(CancellationToken cancellationToken)
    {
        var result = await _service.DisconnectAsync(cancellationToken);
        return Ok(result);
    }

    private static string BuildLandingPage(string title, string message) => $"""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="utf-8"><title>{title}</title></head>
        <body style="font-family: sans-serif; text-align: center; padding-top: 4rem;">
        <h1>{title}</h1>
        <p>{message}</p>
        </body>
        </html>
        """;
}
