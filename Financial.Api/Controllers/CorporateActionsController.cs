using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages corporate actions (splits, mergers, and spin-offs) recorded against an asset.
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

    /// <summary>Records a new merger, closing the source holding and growing the target holding.</summary>
    /// <param name="request">The merger to create.</param>
    /// <returns>200 OK with the updated source and target asset details, or 400 Bad Request if the request is invalid.</returns>
    [HttpPost("merger")]
    [ProducesResponseType(typeof(CorporateActionMergerResultDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CorporateActionMergerResultDTO>> AddMerger([FromBody] CorporateActionMergerCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var result = await _corporateActionService.AddMergerAsync(request);
        return OkOrBadRequest(result);
    }

    /// <summary>Updates an existing merger.</summary>
    /// <param name="request">The merger fields to update.</param>
    /// <returns>200 OK with the updated source and target asset details, or 400 Bad Request if the request is invalid.</returns>
    [HttpPut("merger")]
    [ProducesResponseType(typeof(CorporateActionMergerResultDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CorporateActionMergerResultDTO>> UpdateMerger([FromBody] CorporateActionMergerUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var result = await _corporateActionService.UpdateMergerAsync(request);
        return OkOrBadRequest(result);
    }

    /// <summary>Records a new spin-off, keeping the parent holding open and creating/growing the new holding.</summary>
    /// <param name="request">The spin-off to create.</param>
    /// <returns>200 OK with the updated parent and new asset details, or 400 Bad Request if the request is invalid.</returns>
    [HttpPost("spin-off")]
    [ProducesResponseType(typeof(CorporateActionSpinOffResultDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CorporateActionSpinOffResultDTO>> AddSpinOff([FromBody] CorporateActionSpinOffCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var result = await _corporateActionService.AddSpinOffAsync(request);
        return OkOrBadRequest(result);
    }

    /// <summary>Updates an existing spin-off.</summary>
    /// <param name="request">The spin-off fields to update.</param>
    /// <returns>200 OK with the updated parent and new asset details, or 400 Bad Request if the request is invalid.</returns>
    [HttpPut("spin-off")]
    [ProducesResponseType(typeof(CorporateActionSpinOffResultDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CorporateActionSpinOffResultDTO>> UpdateSpinOff([FromBody] CorporateActionSpinOffUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var result = await _corporateActionService.UpdateSpinOffAsync(request);
        return OkOrBadRequest(result);
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

        var asset = await _corporateActionService.DeleteCorporateActionAsync(request);
        return OkOrBadRequest(asset);
    }
}
