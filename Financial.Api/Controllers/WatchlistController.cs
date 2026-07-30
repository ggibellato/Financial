using Financial.Investment.Application.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Financial.Api.Controllers;

/// <summary>
/// Provides the user-configured watchlist of assets to keep an eye on.
/// </summary>
[ApiController]
[Route("watchlist")]
public sealed class WatchlistController : ControllerBase
{
    private readonly WatchlistOptions _options;

    public WatchlistController(IOptions<WatchlistOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>Lists the configured watchlist items.</summary>
    /// <returns>200 OK with the watchlist items.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WatchlistItem>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<WatchlistItem>> Get() => Ok(_options.Items);
}
