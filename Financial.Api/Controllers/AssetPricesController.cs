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

    public AssetPricesController(IAssetPriceService assetPriceService)
    {
        _assetPriceService = assetPriceService ?? throw new ArgumentNullException(nameof(assetPriceService));
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

        try
        {
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
        catch (ArgumentException)
        {
            return BadRequest();
        }
    }
}
