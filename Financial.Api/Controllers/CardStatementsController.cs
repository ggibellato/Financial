using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("card-statements")]
public sealed class CardStatementsController : ControllerBase
{
    private readonly ICardStatementService _cardStatementService;

    public CardStatementsController(ICardStatementService cardStatementService)
    {
        _cardStatementService = cardStatementService ?? throw new ArgumentNullException(nameof(cardStatementService));
    }

    [HttpGet("{year:int}/{month:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<CardStatementDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CardStatementDTO>>> GetStatementsForMonth(int year, int month)
    {
        var result = await _cardStatementService.GetStatementsForMonthAsync(year, month);
        return Ok(result);
    }

    [HttpPost("{id:guid}/mark-paid")]
    [ProducesResponseType(typeof(CardStatementDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CardStatementDTO>> MarkStatementPaid(Guid id)
    {
        try
        {
            var result = await _cardStatementService.MarkStatementPaidAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }
}
