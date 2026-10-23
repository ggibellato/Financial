using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("accounts")]
public sealed class AccountsController : ControllerBase
{
    private readonly IAccountQueryService _accountQueryService;

    public AccountsController(IAccountQueryService accountQueryService)
    {
        _accountQueryService = accountQueryService ?? throw new ArgumentNullException(nameof(accountQueryService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<AccountDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<AccountDTO>> GetAccounts() =>
        Ok(_accountQueryService.GetAccounts());

    [HttpGet("{id:guid}/{year:int}/{month:int}")]
    [ProducesResponseType(typeof(AccountDetailDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<AccountDetailDTO> GetAccount(Guid id, int year, int month) =>
        Ok(_accountQueryService.GetAccount(id, year, month));

    [HttpGet("{id:guid}/transactions/{year:int}/{month:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<AccountTransactionDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IReadOnlyList<AccountTransactionDTO>> GetTransactions(Guid id, int year, int month) =>
        Ok(_accountQueryService.GetTransactions(id, year, month));
}
