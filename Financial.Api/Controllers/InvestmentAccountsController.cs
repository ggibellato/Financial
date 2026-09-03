using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages investment accounts.
/// </summary>
[ApiController]
[Route("investment-accounts")]
public sealed class InvestmentAccountsController : ControllerBase
{
    private readonly IInvestmentAccountService _investmentAccountService;

    public InvestmentAccountsController(IInvestmentAccountService investmentAccountService)
    {
        _investmentAccountService = investmentAccountService ?? throw new ArgumentNullException(nameof(investmentAccountService));
    }

    /// <summary>Lists all investment accounts.</summary>
    /// <returns>200 OK with the full, unfiltered list of investment accounts.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InvestmentAccountDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<InvestmentAccountDTO>> GetInvestmentAccounts()
    {
        var result = _investmentAccountService.GetInvestmentAccounts();
        return Ok(result);
    }

    /// <summary>Creates a new investment account.</summary>
    /// <param name="request">The account's name, active flag, and liability flag.</param>
    /// <returns>200 OK with the created account, or 400 Bad Request if the request is invalid.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(InvestmentAccountDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InvestmentAccountDTO>> CreateInvestmentAccount([FromBody] InvestmentAccountCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var account = await _investmentAccountService.CreateInvestmentAccountAsync(request);
        return Ok(account);
    }

    /// <summary>Updates an investment account's name, active flag, and liability flag.</summary>
    /// <param name="id">The account's identifier.</param>
    /// <param name="request">The new field values.</param>
    /// <returns>200 OK with the updated account, 400 Bad Request if the request is invalid, or 404 Not Found if no such account exists.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(InvestmentAccountDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvestmentAccountDTO>> UpdateInvestmentAccount(Guid id, [FromBody] InvestmentAccountUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var account = await _investmentAccountService.UpdateInvestmentAccountAsync(id, request);
        return Ok(account);
    }

    /// <summary>Deletes an investment account, when it has no recorded non-zero investment snapshot.</summary>
    /// <param name="id">The account's identifier.</param>
    /// <returns>200 OK if deleted, 404 Not Found if no such account exists, or 409 Conflict if it has a non-zero balance.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteInvestmentAccount(Guid id)
    {
        await _investmentAccountService.DeleteInvestmentAccountAsync(id);
        return Ok();
    }
}
