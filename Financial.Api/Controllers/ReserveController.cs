using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Exceptions;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("reserve")]
public sealed class ReserveController : ControllerBase
{
    private readonly IReserveService _reserveService;

    public ReserveController(IReserveService reserveService)
    {
        _reserveService = reserveService ?? throw new ArgumentNullException(nameof(reserveService));
    }

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

    [HttpGet("balances")]
    [ProducesResponseType(typeof(IReadOnlyList<ReserveBucketBalanceDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<ReserveBucketBalanceDTO>> GetBucketBalances()
    {
        return Ok(_reserveService.GetBucketBalances());
    }

    [HttpGet("movements")]
    [ProducesResponseType(typeof(IReadOnlyList<ReserveMovementDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<ReserveMovementDTO>> GetMovementHistory()
    {
        return Ok(_reserveService.GetMovementHistory());
    }
}
