using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("accounts")]
public sealed class SettlementController : ControllerBase
{
    private readonly ISettlementService _settlementService;

    public SettlementController(ISettlementService settlementService)
    {
        _settlementService = settlementService ?? throw new ArgumentNullException(nameof(settlementService));
    }

    [HttpPost("{accountId:guid}/settlement-entries")]
    [ProducesResponseType(typeof(SettlementEntryDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SettlementEntryDTO>> AddEntry(Guid accountId, [FromBody] SettlementEntryCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        return StatusCode(StatusCodes.Status201Created, await _settlementService.AddEntryAsync(accountId, request));
    }

    [HttpPut("{accountId:guid}/settlement-entries/{entryId:guid}")]
    [ProducesResponseType(typeof(SettlementEntryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SettlementEntryDTO>> UpdateEntry(Guid accountId, Guid entryId, [FromBody] SettlementEntryUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        return Ok(await _settlementService.UpdateEntryAsync(accountId, entryId, request));
    }

    [HttpDelete("{accountId:guid}/settlement-entries/{entryId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEntry(Guid accountId, Guid entryId)
    {
        await _settlementService.DeleteEntryAsync(accountId, entryId);
        return NoContent();
    }

    [HttpPost("{accountId:guid}/settlements")]
    [ProducesResponseType(typeof(SettlementEntryDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SettlementEntryDTO>> Settle(Guid accountId, [FromBody] SettleRequestDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        return StatusCode(StatusCodes.Status201Created, await _settlementService.SettleAsync(accountId, request));
    }
}
