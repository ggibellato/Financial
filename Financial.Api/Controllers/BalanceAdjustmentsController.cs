using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("accounts")]
public sealed class BalanceAdjustmentsController : ControllerBase
{
    private readonly IBalanceAdjustmentService _balanceAdjustmentService;

    public BalanceAdjustmentsController(IBalanceAdjustmentService balanceAdjustmentService)
    {
        _balanceAdjustmentService = balanceAdjustmentService ?? throw new ArgumentNullException(nameof(balanceAdjustmentService));
    }

    [HttpPost("{accountId:guid}/adjustments")]
    [ProducesResponseType(typeof(BalanceAdjustmentDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BalanceAdjustmentDTO>> AddAdjustment(Guid accountId, [FromBody] BalanceAdjustmentCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        return StatusCode(StatusCodes.Status201Created, await _balanceAdjustmentService.AddAdjustmentAsync(accountId, request));
    }

    [HttpPut("{accountId:guid}/adjustments/{adjustmentId:guid}")]
    [ProducesResponseType(typeof(BalanceAdjustmentDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BalanceAdjustmentDTO>> UpdateAdjustment(Guid accountId, Guid adjustmentId, [FromBody] BalanceAdjustmentUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        return Ok(await _balanceAdjustmentService.UpdateAdjustmentAsync(accountId, adjustmentId, request));
    }

    [HttpDelete("{accountId:guid}/adjustments/{adjustmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAdjustment(Guid accountId, Guid adjustmentId)
    {
        await _balanceAdjustmentService.DeleteAdjustmentAsync(accountId, adjustmentId);
        return NoContent();
    }
}
