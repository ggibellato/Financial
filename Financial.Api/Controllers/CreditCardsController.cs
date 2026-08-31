using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages tracked credit cards.
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

    /// <summary>Creates a new credit card.</summary>
    /// <param name="request">The card's name and active flag.</param>
    /// <returns>200 OK with the created card, or 400 Bad Request if the request is invalid.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(CreditCardDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreditCardDTO>> CreateCreditCard([FromBody] CreditCardCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var creditCard = await _creditCardService.CreateCreditCardAsync(request);
        return Ok(creditCard);
    }

    /// <summary>Updates a credit card's name, active flag, and next invoice due date.</summary>
    /// <param name="id">The credit card's identifier.</param>
    /// <param name="request">The new name, active flag, and due date.</param>
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

    /// <summary>Deletes a credit card, when it has no statement or expense referencing it.</summary>
    /// <param name="id">The credit card's identifier.</param>
    /// <returns>200 OK if deleted, 404 Not Found if no such card exists, or 409 Conflict if it is still referenced.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteCreditCard(Guid id)
    {
        await _creditCardService.DeleteCreditCardAsync(id);
        return Ok();
    }
}
