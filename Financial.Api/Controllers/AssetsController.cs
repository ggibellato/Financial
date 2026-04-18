using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace Financial.Api.Controllers;

[ApiController]
[Route("assets")]
public sealed class AssetsController : ControllerBase
{
    private readonly INavigationService _navigationService;

    public AssetsController(INavigationService navigationService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

    [HttpGet("{brokerName}/{portfolioName}/{assetName}")]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<AssetDetailsDTO> GetAssetDetails(
        string brokerName,
        string portfolioName,
        string assetName)
    {
        var asset = _navigationService.GetAssetDetails(brokerName, portfolioName, assetName);
        if (asset is null)
        {
            return NotFound();
        }

        return Ok(asset);
    }
}
