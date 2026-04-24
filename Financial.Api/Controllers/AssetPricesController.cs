using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

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
    public ActionResult<AssetPriceDTO> GetCurrentPrice([FromQuery] string? exchange, [FromQuery] string? ticker)
    {
        if (string.IsNullOrWhiteSpace(exchange) || string.IsNullOrWhiteSpace(ticker))
        {
            return BadRequest();
        }

        var result = _assetPriceService.GetCurrentPrice(new AssetPriceRequestDTO
        {
            Exchange = exchange.Trim(),
            Ticker = ticker.Trim()
        });

        return Ok(result);
    }
}
