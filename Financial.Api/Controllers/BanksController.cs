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
}
