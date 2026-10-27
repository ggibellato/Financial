using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("accounts")]
public sealed class AccountsController : ControllerBase
{
    private readonly IAccountQueryService _accountQueryService;
    private readonly IAccountAdminService _accountAdminService;

    public AccountsController(IAccountQueryService accountQueryService, IAccountAdminService accountAdminService)
    {
        _accountQueryService = accountQueryService ?? throw new ArgumentNullException(nameof(accountQueryService));
        _accountAdminService = accountAdminService ?? throw new ArgumentNullException(nameof(accountAdminService));
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

    [HttpGet("{id:guid}/statements")]
    [ProducesResponseType(typeof(IReadOnlyList<AccountStatementDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IReadOnlyList<AccountStatementDTO>> GetStatements(Guid id) =>
        Ok(_accountQueryService.GetStatements(id));

    [HttpGet("payable-statements")]
    [ProducesResponseType(typeof(IReadOnlyList<PayableStatementDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<PayableStatementDTO>> GetPayableStatements() =>
        Ok(_accountQueryService.GetPayableStatements());

    [HttpGet("templates")]
    [ProducesResponseType(typeof(IReadOnlyList<AccountTemplateDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<AccountTemplateDTO>> GetTemplates() =>
        Ok(_accountAdminService.GetTemplates());

    [HttpPost]
    [ProducesResponseType(typeof(AccountDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccountDTO>> CreateAccount([FromBody] AccountCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var account = await _accountAdminService.CreateAsync(request);
        return StatusCode(StatusCodes.Status201Created, account);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(AccountDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AccountDTO>> UpdateAccount(Guid id, [FromBody] AccountUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        return Ok(await _accountAdminService.UpdateAsync(id, request));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteAccount(Guid id)
    {
        await _accountAdminService.DeleteAsync(id);
        return NoContent();
    }
}
