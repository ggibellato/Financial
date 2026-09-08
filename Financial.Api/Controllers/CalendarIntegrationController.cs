using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages the optional calendar connection used to sync credit-card due dates. Depends only on
/// <see cref="ICalendarIntegrationService"/> - provider-agnostic by design. Google is the only
/// concrete provider wired in today (see <c>GoogleCalendarProviderAdapter</c>), but that choice
/// lives entirely in DI registration; this controller, its route, and its DTOs carry no
/// Google-specific concept, so swapping the provider later needs no change here.
/// </summary>
[ApiController]
[Route("integrations/calendar")]
public sealed class CalendarIntegrationController : ControllerBase
{
    private readonly ICalendarIntegrationService _service;
    private readonly ICreditCardCalendarSyncService _syncService;

    public CalendarIntegrationController(ICalendarIntegrationService service, ICreditCardCalendarSyncService syncService)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _syncService = syncService ?? throw new ArgumentNullException(nameof(syncService));
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

    /// <summary>The OAuth redirect target. Completes the connection, syncs every qualifying
    /// credit card so pre-existing cards show up immediately rather than staying "Pending"
    /// until their next save, and renders a landing page telling the user to return to the
    /// app - never called by either front end's API client.</summary>
    /// <returns>200 OK with a minimal HTML success/failure page.</returns>
    [HttpGet("callback")]
    [Produces("text/html")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ContentResult> Callback(
        [FromQuery] string? code, [FromQuery] string? state, [FromQuery] string? error, CancellationToken cancellationToken)
    {
        var result = await _service.CompleteConnectionAsync(code, state, error, cancellationToken);
        if (result.Success)
        {
            await _syncService.ResyncAllAsync(cancellationToken);
        }

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

    /// <summary>Manually retries the calendar-event sync for one credit card.</summary>
    /// <returns>200 OK with the resulting status, or 404 if the card does not exist.</returns>
    [HttpPost("credit-cards/{id:guid}/resync")]
    [ProducesResponseType(typeof(CreditCardCalendarSyncStatusDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreditCardCalendarSyncStatusDTO>> ResyncCreditCard(Guid id, CancellationToken cancellationToken)
    {
        var result = await _syncService.ResyncAsync(id, cancellationToken);
        return Ok(result);
    }

    /// <summary>Manually retries the calendar-event sync for every active credit card with a due date.</summary>
    /// <returns>200 OK with the resulting status per card.</returns>
    [HttpPost("resync-all")]
    [ProducesResponseType(typeof(IReadOnlyList<CreditCardCalendarSyncStatusDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CreditCardCalendarSyncStatusDTO>>> ResyncAll(CancellationToken cancellationToken)
    {
        var results = await _syncService.ResyncAllAsync(cancellationToken);
        return Ok(results);
    }

    /// <summary>Returns the current calendar-event sync status for every credit card that has
    /// been synced this process lifetime.</summary>
    /// <returns>200 OK with the current status list.</returns>
    [HttpGet("credit-cards/sync-status")]
    [ProducesResponseType(typeof(IReadOnlyList<CreditCardCalendarSyncStatusDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<CreditCardCalendarSyncStatusDTO>> GetSyncStatuses()
    {
        return Ok(_syncService.GetSyncStatuses());
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
