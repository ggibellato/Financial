using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("accounts")]
public sealed class StatementDueDatesController : ControllerBase
{
    private readonly IStatementDueDateService _statementDueDateService;

    public StatementDueDatesController(IStatementDueDateService statementDueDateService)
    {
        _statementDueDateService = statementDueDateService ?? throw new ArgumentNullException(nameof(statementDueDateService));
    }

    [HttpPut("{accountId:guid}/statements/{year:int}/{month:int}/due-date")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDueDate(Guid accountId, int year, int month, [FromBody] StatementDueDateUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        await _statementDueDateService.SetDueDateAsync(accountId, year, month, request);
        return NoContent();
    }
}
