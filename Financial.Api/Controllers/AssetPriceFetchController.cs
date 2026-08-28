using Financial.Investment.Application.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Financial.Api.Controllers;

/// <summary>
/// Provides the configured list of portfolios whose asset prices should be auto-fetched.
/// </summary>
[ApiController]
[Route("asset-price-fetch")]
public sealed class AssetPriceFetchController : ControllerBase
{
    private readonly AssetPriceFetchOptions _options;

    public AssetPriceFetchController(IOptions<AssetPriceFetchOptions> options)
    {
        _options = options.Value;
    }

    /// <summary>Lists the portfolios configured for automatic asset price fetching.</summary>
    /// <returns>200 OK with the configured portfolios.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PortfolioReferenceDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<PortfolioReferenceDTO>> Get() => Ok(_options.Portfolios);
}
