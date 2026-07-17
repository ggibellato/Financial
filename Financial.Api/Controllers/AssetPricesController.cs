using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Domain.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("prices")]
public sealed class AssetPricesController : ControllerBase
{
    private readonly IAssetPriceService _assetPriceService;

    public AssetPricesController(IAssetPriceService assetPriceService)
    {
        _assetPriceService = assetPriceService ?? throw new ArgumentNullException(nameof(assetPriceService));
    }

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
