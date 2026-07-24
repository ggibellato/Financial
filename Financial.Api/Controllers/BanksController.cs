using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("banks")]
public sealed class BanksController : ControllerBase
{
    private readonly IBankService _bankService;

    public BanksController(IBankService bankService)
    {
        _bankService = bankService ?? throw new ArgumentNullException(nameof(bankService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BankDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<BankDTO>> GetBanks()
    {
        var result = _bankService.GetBanks();
        return Ok(result);
    }

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
}
