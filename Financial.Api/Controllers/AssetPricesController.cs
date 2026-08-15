using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Looks up current market prices for individual assets.
/// </summary>
[ApiController]
[Route("prices")]
public sealed class AssetPricesController : ControllerBase
{
    private readonly IAssetPriceService _assetPriceService;
    private readonly IPriceService _priceService;

    public AssetPricesController(IAssetPriceService assetPriceService, IPriceService priceService)
    {
        _assetPriceService = assetPriceService ?? throw new ArgumentNullException(nameof(assetPriceService));
        _priceService = priceService ?? throw new ArgumentNullException(nameof(priceService));
    }

    /// <summary>Returns the current market price for an asset.</summary>
    /// <param name="exchange">Optional exchange code.</param>
    /// <param name="ticker">The stock/ETF ticker symbol. Required.</param>
    /// <param name="assetClass">Optional asset class (e.g. "Stock", "Crypto"); defaults to "Unknown" if omitted or unrecognized.</param>
    /// <param name="brokerName">Optional broker name, used for broker-specific pricing sources.</param>
    /// <param name="name">Optional display name, used as a lookup fallback.</param>
    /// <returns>200 OK with the current price, or 400 Bad Request if <paramref name="ticker"/> is missing or invalid.</returns>
    [HttpGet("current")]
    [ProducesResponseType(typeof(AssetPriceDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<AssetPriceDTO> GetCurrentPrice(
        [FromQuery] string? exchange,
        [FromQuery] string? ticker,
        [FromQuery] string? assetClass,
        [FromQuery] string? brokerName,
        [FromQuery] string? name)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            return BadRequest();
        }

        var parsedAssetClass = Enum.TryParse<GlobalAssetClass>(assetClass, ignoreCase: true, out var parsed)
            ? parsed
            : GlobalAssetClass.Unknown;

        var result = _assetPriceService.GetCurrentPrice(new AssetPriceRequestDTO
        {
            Exchange = exchange?.Trim() ?? string.Empty,
            Ticker = ticker.Trim(),
            AssetClass = parsedAssetClass,
            BrokerName = brokerName?.Trim(),
            Name = name?.Trim()
        });

        return Ok(result);
    }

    /// <summary>Records a manual price for an asset on a given date, replacing any existing entry for that date.</summary>
    /// <param name="request">The asset, date, and price to record.</param>
    /// <returns>200 OK with the updated asset details, or 400 Bad Request if the request is invalid.</returns>
    [HttpPut]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetDetailsDTO>> SetPrice([FromBody] SetAssetPriceDTO request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var asset = await _priceService.SetPriceAsync(request);
        if (asset is null)
        {
            return BadRequest();
        }

        return Ok(asset);
    }

    /// <summary>Deletes a manually-entered price for an asset on a given date. A missing date is a no-op; an automatic entry can't be deleted this way.</summary>
    /// <param name="request">Identifies the asset and date to clear.</param>
    /// <returns>200 OK with the updated asset details, or 400 Bad Request if the request is invalid.</returns>
    [HttpDelete]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetDetailsDTO>> DeletePrice([FromBody] DeleteAssetPriceDTO request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var asset = await _priceService.DeletePriceAsync(request);
        if (asset is null)
        {
            return BadRequest();
        }

        return Ok(asset);
    }
}
