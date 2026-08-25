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
    private readonly IAssetMoveService _assetMoveService;

    public AssetsController(INavigationService navigationService, IAssetMoveService assetMoveService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _assetMoveService = assetMoveService ?? throw new ArgumentNullException(nameof(assetMoveService));
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

    /// <summary>Moves an asset into another portfolio of the same broker.</summary>
    /// <param name="request">The asset to move and where it should go. The destination portfolio is created when the name is one the broker does not have yet.</param>
    /// <returns>
    /// 200 OK with the moved asset read back from its new portfolio,
    /// 400 Bad Request if a field or the destination name is blank,
    /// 404 Not Found if the broker, portfolio or asset does not exist,
    /// or 409 Conflict if a move rule refused it.
    /// </returns>
    [HttpPost("move")]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetDetailsDTO>> MoveAsset([FromBody] MoveAssetRequestDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        return Ok(await _assetMoveService.MoveAssetAsync(request));
    }

    /// <summary>Retires a fully closed asset from Active Investments into a Historic portfolio.</summary>
    /// <param name="request">The asset to archive and the Historic portfolio to put it in, existing or to be created.</param>
    /// <returns>
    /// 200 OK with the archived asset read back from Historic Investments,
    /// 400 Bad Request if a required field is blank,
    /// 404 Not Found if the broker, portfolio or asset does not exist,
    /// or 409 Conflict if the asset still holds a position or the destination refuses it.
    /// </returns>
    /// <remarks>
    /// Its own endpoint rather than a scope pair on the move: the direction is fixed, so there is no
    /// way to ask for the reverse.
    /// </remarks>
    [HttpPost("archive")]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AssetDetailsDTO>> ArchiveAsset([FromBody] ArchiveAssetRequestDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        return Ok(await _assetMoveService.ArchiveAssetAsync(request));
    }
}
