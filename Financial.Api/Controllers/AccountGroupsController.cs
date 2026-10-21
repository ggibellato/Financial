using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("accounts/groups")]
public sealed class AccountGroupsController : ControllerBase
{
    private readonly IAccountGroupSummaryService _accountGroupSummaryService;

    public AccountGroupsController(IAccountGroupSummaryService accountGroupSummaryService)
    {
        _accountGroupSummaryService = accountGroupSummaryService ?? throw new ArgumentNullException(nameof(accountGroupSummaryService));
    }

    [HttpGet("default/summary/{year:int}/{month:int}")]
    [ProducesResponseType(typeof(AccountGroupSummaryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<AccountGroupSummaryDTO> GetDefaultGroupSummary(int year, int month) =>
        Ok(_accountGroupSummaryService.GetDefaultGroupSummary(year, month));
}
