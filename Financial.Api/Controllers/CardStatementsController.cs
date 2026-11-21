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

    /// <summary>Marks a card statement as paid.</summary>
    /// <param name="id">The statement's identifier.</param>
    /// <param name="request">Details of the payment.</param>
    [HttpPost("{id:guid}/mark-paid")]
    [ProducesResponseType(typeof(CardStatementDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CardStatementDTO>> MarkStatementPaid(Guid id, [FromBody] MarkCardStatementPaidDTO request)
    {
        var result = await _cardStatementService.MarkStatementPaidAsync(id, request);
        return Ok(result);
    }

    /// <summary>Reverts a card statement back to unpaid.</summary>
    /// <param name="id">The statement's identifier.</param>
    [HttpPost("{id:guid}/unmark-paid")]
    [ProducesResponseType(typeof(CardStatementDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CardStatementDTO>> UnmarkStatementPaid(Guid id)
    {
        var result = await _cardStatementService.UnmarkStatementPaidAsync(id);
        return Ok(result);
    }

    /// <summary>Settles a statement that has nothing to pay, writing its pot withdrawals' investment withdrawals without a card payment.</summary>
    /// <param name="id">The statement's identifier.</param>
    /// <param name="request">The bank and date of the investment withdrawals.</param>
    [HttpPost("{id:guid}/settle-pot-withdrawals")]
    [ProducesResponseType(typeof(CardStatementDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CardStatementDTO>> SettlePotWithdrawals(Guid id, [FromBody] SettlePotWithdrawalsDTO request)
    {
        var result = await _cardStatementService.SettlePotWithdrawalsAsync(id, request);
        return Ok(result);
    }

    /// <summary>Reverts a settled statement, removing the investment withdrawals its settlement wrote.</summary>
    /// <param name="id">The statement's identifier.</param>
    [HttpPost("{id:guid}/unsettle-pot-withdrawals")]
    [ProducesResponseType(typeof(CardStatementDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CardStatementDTO>> UnsettlePotWithdrawals(Guid id)
    {
        var result = await _cardStatementService.UnsettlePotWithdrawalsAsync(id);
        return Ok(result);
    }
}
