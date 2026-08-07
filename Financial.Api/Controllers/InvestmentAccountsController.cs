using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Read-only access to the seeded investment accounts.
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
}
