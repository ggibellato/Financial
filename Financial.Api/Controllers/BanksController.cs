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

    public BanksController(IBankService bankService)
    {
        _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
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
            var bank = await _bankService.UpdateOpeningBalanceAsync(name, request);
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
}
