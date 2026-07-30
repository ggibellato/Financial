using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Provides read access to individual investment asset details.
/// </summary>
[ApiController]
[Route("assets")]
public sealed class AssetsController : ControllerBase
{
    private readonly INavigationService _navigationService;

    public AssetsController(INavigationService navigationService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

    /// <summary>Returns the full details for a single asset.</summary>
    /// <param name="brokerName">The broker's name.</param>
    /// <param name="portfolioName">The portfolio's name.</param>
    /// <param name="assetName">The asset's name.</param>
    /// <param name="scope">Optional investment scope filter (e.g. "all", "active-only").</param>
    /// <returns>200 OK with the asset details, or 404 Not Found if no such asset exists.</returns>
    [HttpGet("{brokerName}/{portfolioName}/{assetName}")]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<AssetDetailsDTO> GetAssetDetails(
        string brokerName,
        string portfolioName,
        string assetName,
        [FromQuery] string? scope)
    {
        var asset = _navigationService.GetAssetDetails(brokerName, portfolioName, assetName, InvestmentScopeParser.ParseOrDefault(scope));
        if (asset is null)
        {
            return NotFound();
        }

        return Ok(asset);
    }
}
