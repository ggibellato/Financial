using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("accounts")]
public sealed class PotsController : ControllerBase
{
    private readonly IPotsService _potsService;

    public PotsController(IPotsService potsService)
    {
        _potsService = potsService ?? throw new ArgumentNullException(nameof(potsService));
    }

    [HttpPost("{potsId:guid}/deposits")]
    [ProducesResponseType(typeof(PotOperationDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PotOperationDTO>> Deposit(Guid potsId, [FromBody] PotDepositRequestDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        return StatusCode(StatusCodes.Status201Created, await _potsService.DepositAsync(potsId, request));
    }

    [HttpPut("{potsId:guid}/deposits/{operationId:guid}")]
    [ProducesResponseType(typeof(PotOperationDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PotOperationDTO>> UpdateDeposit(Guid potsId, Guid operationId, [FromBody] PotDepositRequestDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        return Ok(await _potsService.UpdateDepositAsync(potsId, operationId, request));
    }

    [HttpPost("{potsId:guid}/withdrawals")]
    [ProducesResponseType(typeof(PotOperationDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PotOperationDTO>> Withdraw(Guid potsId, [FromBody] PotWithdrawalRequestDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        return StatusCode(StatusCodes.Status201Created, await _potsService.WithdrawAsync(potsId, request));
    }

    [HttpPut("{potsId:guid}/withdrawals/{operationId:guid}")]
    [ProducesResponseType(typeof(PotOperationDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PotOperationDTO>> UpdateWithdrawal(Guid potsId, Guid operationId, [FromBody] PotWithdrawalRequestDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        return Ok(await _potsService.UpdateWithdrawalAsync(potsId, operationId, request));
    }

    [HttpGet("operations/{operationId:guid}")]
    [ProducesResponseType(typeof(PotOperationDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<PotOperationDTO> GetOperation(Guid operationId) =>
        Ok(_potsService.GetOperation(operationId));

    [HttpDelete("operations/{operationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteOperation(Guid operationId)
    {
        await _potsService.DeleteOperationAsync(operationId);
        return NoContent();
    }
}
