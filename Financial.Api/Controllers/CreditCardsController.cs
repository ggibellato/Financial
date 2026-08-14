using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages the seeded credit cards' due date and active flag.
/// </summary>
[ApiController]
[Route("credit-cards")]
public sealed class CreditCardsController : ControllerBase
{
    private readonly ICreditCardService _creditCardService;

    public CreditCardsController(ICreditCardService creditCardService)
    {
        _creditCardService = creditCardService ?? throw new ArgumentNullException(nameof(creditCardService));
    }

    /// <summary>Lists all credit cards, active and inactive.</summary>
    /// <returns>200 OK with the full list of credit cards.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CreditCardDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<CreditCardDTO>> GetCreditCards()
    {
        var result = _creditCardService.GetCreditCards();
        return Ok(result);
    }

    /// <summary>Updates a credit card's next invoice due date and active flag.</summary>
    /// <param name="id">The credit card's identifier.</param>
    /// <param name="request">The new due date and active flag.</param>
    /// <returns>200 OK with the updated credit card, 400 Bad Request if the request is invalid, or 404 Not Found if no such card exists.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CreditCardDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreditCardDTO>> UpdateCreditCard(Guid id, [FromBody] CreditCardUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var creditCard = await _creditCardService.UpdateCreditCardAsync(id, request);
        return Ok(creditCard);
    }
}
