using Financial.Application.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Financial.Api.Controllers;

[ApiController]
[Route("watchlist")]
public sealed class WatchlistController : ControllerBase
{
    private readonly WatchlistOptions _options;

    public WatchlistController(IOptions<WatchlistOptions> options)
    {
        _options = options.Value;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WatchlistItem>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<WatchlistItem>> Get() => Ok(_options.Items);
}
