using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages the optional Google Calendar connection used to sync credit-card due dates.
/// </summary>
[ApiController]
[Route("integrations/google-calendar")]
public sealed class GoogleCalendarIntegrationController : ControllerBase
{
    private readonly IGoogleCalendarIntegrationService _service;

    public GoogleCalendarIntegrationController(IGoogleCalendarIntegrationService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>Returns the current connection status.</summary>
    /// <returns>200 OK with the connection status.</returns>
    [HttpGet("status")]
    [ProducesResponseType(typeof(GoogleCalendarConnectionStatusDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<GoogleCalendarConnectionStatusDTO>> GetStatus(CancellationToken cancellationToken)
    {
        var status = await _service.GetStatusAsync(cancellationToken);
        return Ok(status);
    }

    /// <summary>Redirects the browser to Google's consent screen.</summary>
    /// <returns>302 redirect to Google's OAuth consent URL.</returns>
    [HttpGet("connect")]
    [ProducesResponseType(StatusCodes.Status302Found)]
    public IActionResult Connect()
    {
        var url = _service.BuildAuthorizationUrl();
        return Redirect(url);
    }

    /// <summary>Google's OAuth redirect target. Completes the connection and renders a landing
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

    /// <summary>Removes the Google Calendar connection - deletes the dedicated calendar, revokes
    /// the token, and clears local state, always clearing local state even if the remote calls fail.</summary>
    /// <returns>200 OK with whether the remote cleanup succeeded.</returns>
    [HttpPost("disconnect")]
    [ProducesResponseType(typeof(GoogleCalendarDisconnectResultDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<GoogleCalendarDisconnectResultDTO>> Disconnect(CancellationToken cancellationToken)
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
