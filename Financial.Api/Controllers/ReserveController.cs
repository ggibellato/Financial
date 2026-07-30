using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages emergency/savings reserve buckets: income splits, withdrawals, balances and movement history.
/// </summary>
[ApiController]
[Route("reserve")]
public sealed class ReserveController : ControllerBase
{
    private readonly IReserveService _reserveService;

    public ReserveController(IReserveService reserveService)
    {
        _reserveService = reserveService ?? throw new ArgumentNullException(nameof(reserveService));
    }

    /// <summary>Splits an income across reserve buckets according to the configured allocation rules.</summary>
    /// <param name="request">The income amount to split.</param>
    /// <returns>200 OK with the resulting split, or 400 Bad Request if the request is invalid or missing.</returns>
    [HttpPost("income-split")]
    [ProducesResponseType(typeof(IncomeSplitResultDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IncomeSplitResultDTO>> PostIncomeSplit([FromBody] IncomeSplitRequestDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var result = await _reserveService.PostIncomeSplitAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>Withdraws an amount from a reserve bucket.</summary>
    /// <param name="request">The withdrawal amount and target bucket.</param>
    /// <returns>200 OK with the resulting movement, 400 Bad Request if the request is invalid, or 409 Conflict if the withdrawal would overdraft the bucket without confirmation.</returns>
    [HttpPost("withdrawals")]
    [ProducesResponseType(typeof(ReserveMovementDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReserveMovementDTO>> PostWithdrawal([FromBody] WithdrawalRequestDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var result = await _reserveService.PostWithdrawalAsync(request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (OverdraftConfirmationRequiredException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status409Conflict);
        }
    }

    /// <summary>Lists the current balance of each reserve bucket.</summary>
    /// <returns>200 OK with the bucket balances.</returns>
    [HttpGet("balances")]
    [ProducesResponseType(typeof(IReadOnlyList<ReserveBucketBalanceDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<ReserveBucketBalanceDTO>> GetBucketBalances()
    {
        return Ok(_reserveService.GetBucketBalances());
    }

    /// <summary>Lists the full reserve movement history (splits and withdrawals).</summary>
    /// <returns>200 OK with the movement history.</returns>
    [HttpGet("movements")]
    [ProducesResponseType(typeof(IReadOnlyList<ReserveMovementDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<ReserveMovementDTO>> GetMovementHistory()
    {
        return Ok(_reserveService.GetMovementHistory());
    }

    /// <summary>Updates an existing reserve movement.</summary>
    /// <param name="id">The movement's identifier.</param>
    /// <param name="request">The new movement fields.</param>
    /// <returns>200 OK with the updated movement, 400 Bad Request if the request is invalid, or 404 Not Found if the movement doesn't exist.</returns>
    [HttpPut("movements/{id:guid}")]
    [ProducesResponseType(typeof(ReserveMovementDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReserveMovementDTO>> UpdateMovement(Guid id, [FromBody] UpdateReserveMovementDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var result = await _reserveService.UpdateMovementAsync(id, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    /// <summary>Deletes a reserve movement.</summary>
    /// <param name="id">The movement's identifier.</param>
    /// <returns>200 OK if deleted, or 404 Not Found if the movement doesn't exist.</returns>
    [HttpDelete("movements/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteMovement(Guid id)
    {
        try
        {
            await _reserveService.DeleteMovementAsync(id);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }
}
