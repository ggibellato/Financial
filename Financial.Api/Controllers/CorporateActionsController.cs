using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages corporate actions (splits, and future merger/spin-off types) recorded against an asset.
/// </summary>
[ApiController]
[Route("corporate-actions")]
public sealed class CorporateActionsController : ApiControllerBase
{
    private readonly ICorporateActionService _corporateActionService;

    public CorporateActionsController(ICorporateActionService corporateActionService)
    {
        _corporateActionService = corporateActionService ?? throw new ArgumentNullException(nameof(corporateActionService));
    }

    /// <summary>Records a new split or reverse split.</summary>
    /// <param name="request">The split to create.</param>
    /// <returns>200 OK with the updated asset details, or 400 Bad Request if the request is invalid.</returns>
    [HttpPost("split")]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetDetailsDTO>> AddSplit([FromBody] CorporateActionSplitCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var asset = await _corporateActionService.AddSplitAsync(request);
        return OkOrBadRequest(asset);
    }

    /// <summary>Updates an existing split or reverse split.</summary>
    /// <param name="request">The split fields to update.</param>
    /// <returns>200 OK with the updated asset details, or 400 Bad Request if the request is invalid.</returns>
    [HttpPut("split")]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetDetailsDTO>> UpdateSplit([FromBody] CorporateActionSplitUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var asset = await _corporateActionService.UpdateSplitAsync(request);
        return OkOrBadRequest(asset);
    }

    /// <summary>Deletes a corporate action.</summary>
    /// <param name="request">Identifies the corporate action to delete.</param>
    /// <returns>200 OK with the updated asset details, or 400 Bad Request if the request is invalid.</returns>
    [HttpDelete]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetDetailsDTO>> DeleteCorporateAction([FromBody] CorporateActionDeleteDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var asset = await _corporateActionService.DeleteSplitAsync(request);
        return OkOrBadRequest(asset);
    }
}
