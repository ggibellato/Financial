using Financial.Application.Configuration;
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
    [ProducesResponseType(typeof(IReadOnlyList<AssetPriceFetch>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<AssetPriceFetch>> Get() => Ok(_options.Portfolios);
}
