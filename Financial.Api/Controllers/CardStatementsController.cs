using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages credit card statements and their paid/unpaid state.
/// </summary>
[ApiController]
[Route("card-statements")]
public sealed class CardStatementsController : ControllerBase
{
    private readonly ICardStatementService _cardStatementService;

    public CardStatementsController(ICardStatementService cardStatementService)
    {
        _cardStatementService = cardStatementService ?? throw new ArgumentNullException(nameof(cardStatementService));
    }

    /// <summary>Lists card statements due in a given month.</summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <returns>200 OK with the matching statements.</returns>
    [HttpGet("{year:int}/{month:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<CardStatementDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CardStatementDTO>>> GetStatementsForMonth(int year, int month)
    {
        var result = await _cardStatementService.GetStatementsForMonthAsync(year, month);
        return Ok(result);
    }

    /// <summary>Marks a card statement as paid.</summary>
    /// <param name="id">The statement's identifier.</param>
    /// <param name="request">Details of the payment.</param>
    /// <returns>200 OK with the updated statement, 400 Bad Request if the request is invalid, or 404 Not Found if the statement doesn't exist.</returns>
    [HttpPost("{id:guid}/mark-paid")]
    [ProducesResponseType(typeof(CardStatementDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CardStatementDTO>> MarkStatementPaid(Guid id, [FromBody] MarkStatementPaidDTO request)
    {
        var result = await _cardStatementService.MarkStatementPaidAsync(id, request);
        return Ok(result);
    }

    /// <summary>Reverts a card statement back to unpaid.</summary>
    /// <param name="id">The statement's identifier.</param>
    /// <returns>200 OK with the updated statement, or 404 Not Found if the statement doesn't exist.</returns>
    [HttpPost("{id:guid}/unmark-paid")]
    [ProducesResponseType(typeof(CardStatementDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CardStatementDTO>> UnmarkStatementPaid(Guid id)
    {
        var result = await _cardStatementService.UnmarkStatementPaidAsync(id);
        return Ok(result);
    }
}
