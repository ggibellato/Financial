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

    /// <summary>Updates a bank's opening balance and the date it's accurate as of.</summary>
    /// <param name="name">The bank's name.</param>
    /// <param name="request">The new opening balance and date.</param>
    /// <returns>200 OK with the updated bank, 400 Bad Request if the request is invalid, or 404 Not Found if no such bank exists.</returns>
    [HttpPut("{name}/opening-balance")]
    [ProducesResponseType(typeof(BankDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BankDTO>> UpdateOpeningBalance(string name, [FromBody] BankOpeningBalanceUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var bank = await _bankService.UpdateOpeningBalanceAsync(ResolveBankId(name), request);
            return Ok(bank);
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
    /// <param name="name">The bank's name.</param>
    /// <param name="request">The adjustment to create.</param>
    /// <returns>200 OK with the created adjustment and its computed delta, 400 Bad Request if the request is invalid.</returns>
    [HttpPost("{name}/adjustments")]
    [ProducesResponseType(typeof(BalanceAdjustmentDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BalanceAdjustmentDTO>> AddAdjustment(string name, [FromBody] BalanceAdjustmentCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var adjustment = await _balanceAdjustmentService.AddAdjustmentAsync(ResolveBankId(name), request);
            return Ok(adjustment);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>Updates an existing balance adjustment.</summary>
    /// <param name="name">The bank's name.</param>
    /// <param name="id">The adjustment's identifier.</param>
    /// <param name="request">The new adjustment fields.</param>
    /// <returns>200 OK with the updated adjustment, 400 Bad Request if the request is invalid, or 404 Not Found if the adjustment doesn't exist.</returns>
    [HttpPut("{name}/adjustments/{id:guid}")]
    [ProducesResponseType(typeof(BalanceAdjustmentDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<BalanceAdjustmentDTO>> UpdateAdjustment(string name, Guid id, [FromBody] BalanceAdjustmentUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var adjustment = await _balanceAdjustmentService.UpdateAdjustmentAsync(ResolveBankId(name), id, request);
            return Ok(adjustment);
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

    /// <summary>Deletes a balance adjustment.</summary>
    /// <param name="name">The bank's name.</param>
    /// <param name="id">The adjustment's identifier.</param>
    /// <returns>200 OK if deleted, or 404 Not Found if the adjustment doesn't exist.</returns>
    [HttpDelete("{name}/adjustments/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAdjustment(string name, Guid id)
    {
        try
        {
            await _balanceAdjustmentService.DeleteAdjustmentAsync(ResolveBankId(name), id);
            return Ok();
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

    /// <summary>Lists all balance adjustments for a bank.</summary>
    /// <param name="name">The bank's name.</param>
    /// <returns>200 OK with the matching adjustments.</returns>
    [HttpGet("{name}/adjustments")]
    [ProducesResponseType(typeof(IReadOnlyList<BalanceAdjustmentDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<BalanceAdjustmentDTO>> GetAdjustmentsByBank(string name)
    {
        if (!TryResolveBankId(name, out var bankId))
        {
            return Ok(Array.Empty<BalanceAdjustmentDTO>());
        }

        var result = _balanceAdjustmentService.GetAdjustmentsByBank(bankId);
        return Ok(result);
    }

    private Guid ResolveBankId(string name) =>
        TryResolveBankId(name, out var bankId)
            ? bankId
            : throw new KeyNotFoundException($"Bank '{name}' was not found.");

    private bool TryResolveBankId(string name, out Guid bankId)
    {
        var bank = _bankService.GetBanks().FirstOrDefault(b => b.Name == name);
        bankId = bank?.Id ?? Guid.Empty;
        return bank is not null;
    }
}
