using Financial.Api.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Financial.Api.Controllers;

[ApiController]
[Route("asset-price-fetch")]
public sealed class AssetPriceFetchController : ControllerBase
{
    private readonly AssetPriceFetchOptions _options;

    public AssetPriceFetchController(IOptions<AssetPriceFetchOptions> options)
    {
        _options = options.Value;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PortfolioReference>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<PortfolioReference>> Get() => Ok(_options.Portfolios);
}
