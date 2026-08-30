using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages tracked banks and their opening balances.
/// </summary>
[ApiController]
[Route("banks")]
public sealed class BanksController : ControllerBase
{
    private readonly IBankService _bankService;
    private readonly IBalanceAdjustmentService _balanceAdjustmentService;

    public BanksController(IBankService bankService, IBalanceAdjustmentService balanceAdjustmentService)
    {
        _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
        _balanceAdjustmentService = balanceAdjustmentService ?? throw new ArgumentNullException(nameof(balanceAdjustmentService));
    }

    /// <summary>Lists all tracked banks.</summary>
    /// <returns>200 OK with the list of banks.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BankDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<BankDTO>> GetBanks()
    {
        var result = _bankService.GetBanks();
        return Ok(result);
    }

    /// <summary>Creates a new bank.</summary>
    /// <param name="request">The bank's name and round-up setting.</param>
    /// <returns>200 OK with the created bank, or 400 Bad Request if the request is invalid.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(BankDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BankDTO>> CreateBank([FromBody] BankCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var bank = await _bankService.CreateBankAsync(request);
        return Ok(bank);
    }

    /// <summary>Updates a bank's name and round-up setting.</summary>
    /// <param name="id">The bank's identifier.</param>
    /// <param name="request">The new name and round-up setting.</param>
    /// <returns>200 OK with the updated bank, 400 Bad Request if the request is invalid, or 404 Not Found if no such bank exists.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BankDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BankDTO>> UpdateBank(Guid id, [FromBody] BankUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var bank = await _bankService.UpdateBankAsync(id, request);
        return Ok(bank);
    }

    /// <summary>Deletes a bank, when it has no balance history or transactions referencing it.</summary>
    /// <param name="id">The bank's identifier.</param>
    /// <returns>200 OK if deleted, 404 Not Found if no such bank exists, or 409 Conflict if it is still referenced.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteBank(Guid id)
    {
        await _bankService.DeleteBankAsync(id);
        return Ok();
    }

    /// <summary>Updates a bank's opening balance and the date it's accurate as of.</summary>
    /// <param name="id">The bank's identifier.</param>
    /// <param name="request">The new opening balance and date.</param>
    /// <returns>200 OK with the updated bank, 400 Bad Request if the request is invalid, or 404 Not Found if no such bank exists.</returns>
    [HttpPut("{id:guid}/opening-balance")]
    [ProducesResponseType(typeof(BankDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BankDTO>> UpdateOpeningBalance(Guid id, [FromBody] BankOpeningBalanceUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var bank = await _bankService.UpdateOpeningBalanceAsync(id, request);
        return Ok(bank);
    }

    /// <summary>Returns each bank's balance as of the end of the given month.</summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <returns>200 OK with the per-bank balances.</returns>
    [HttpGet("month/{year:int}/{month:int}/balances")]
    [ProducesResponseType(typeof(IReadOnlyList<BankBalanceDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<BankBalanceDTO>> GetBankBalancesByMonth(int year, int month)
    {
        var result = _bankService.GetBankBalancesByMonth(year, month);
        return Ok(result);
    }

    /// <summary>Records a new balance adjustment for a bank.</summary>
    /// <param name="id">The bank's identifier.</param>
    /// <param name="request">The adjustment to create.</param>
    /// <returns>200 OK with the created adjustment and its computed delta, 400 Bad Request if the request is invalid, or 404 Not Found if no such bank exists.</returns>
    [HttpPost("{id:guid}/adjustments")]
    [ProducesResponseType(typeof(BalanceAdjustmentDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BalanceAdjustmentDTO>> AddAdjustment(Guid id, [FromBody] BalanceAdjustmentCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        if (!BankExists(id))
        {
            return NotFound();
        }

        var adjustment = await _balanceAdjustmentService.AddAdjustmentAsync(id, request);
        return Ok(adjustment);
    }

    /// <summary>Updates an existing balance adjustment.</summary>
    /// <param name="id">The bank's identifier.</param>
    /// <param name="adjustmentId">The adjustment's identifier.</param>
    /// <param name="request">The new adjustment fields.</param>
    /// <returns>200 OK with the updated adjustment, 400 Bad Request if the request is invalid, or 404 Not Found if the bank or adjustment doesn't exist.</returns>
    [HttpPut("{id:guid}/adjustments/{adjustmentId:guid}")]
    [ProducesResponseType(typeof(BalanceAdjustmentDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BalanceAdjustmentDTO>> UpdateAdjustment(Guid id, Guid adjustmentId, [FromBody] BalanceAdjustmentUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        if (!BankExists(id))
        {
            return NotFound();
        }

        var adjustment = await _balanceAdjustmentService.UpdateAdjustmentAsync(id, adjustmentId, request);
        return Ok(adjustment);
    }

    /// <summary>Deletes a balance adjustment.</summary>
    /// <param name="id">The bank's identifier.</param>
    /// <param name="adjustmentId">The adjustment's identifier.</param>
    /// <returns>200 OK if deleted, or 404 Not Found if the bank or adjustment doesn't exist.</returns>
    [HttpDelete("{id:guid}/adjustments/{adjustmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAdjustment(Guid id, Guid adjustmentId)
    {
        if (!BankExists(id))
        {
            return NotFound();
        }

        await _balanceAdjustmentService.DeleteAdjustmentAsync(id, adjustmentId);
        return Ok();
    }

    /// <summary>Lists all balance adjustments for a bank.</summary>
    /// <param name="id">The bank's identifier.</param>
    /// <returns>200 OK with the matching adjustments (empty if the bank doesn't exist).</returns>
    [HttpGet("{id:guid}/adjustments")]
    [ProducesResponseType(typeof(IReadOnlyList<BalanceAdjustmentDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<BalanceAdjustmentDTO>> GetAdjustmentsByBank(Guid id)
    {
        var result = _balanceAdjustmentService.GetAdjustmentsByBank(id);
        return Ok(result);
    }

    private bool BankExists(Guid id) => _bankService.GetBanks().Any(b => b.Id == id);
}
