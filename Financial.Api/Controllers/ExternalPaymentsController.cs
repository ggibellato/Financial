using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("accounts")]
public sealed class ExternalPaymentsController : ControllerBase
{
    private readonly IExternalPaymentService _externalPaymentService;

    public ExternalPaymentsController(IExternalPaymentService externalPaymentService)
    {
        _externalPaymentService = externalPaymentService ?? throw new ArgumentNullException(nameof(externalPaymentService));
    }

    /// <summary>Records money paid into, or out of, the account from outside the app's accounts.</summary>
    [HttpPost("{accountId:guid}/payments")]
    [ProducesResponseType(typeof(ExternalPaymentDTO), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExternalPaymentDTO>> AddPayment(Guid accountId, [FromBody] ExternalPaymentCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        return StatusCode(StatusCodes.Status201Created, await _externalPaymentService.AddPaymentAsync(accountId, request));
    }

    /// <summary>Changes a pay-in or pay-out's date, amount or description.</summary>
    [HttpPut("{accountId:guid}/payments/{paymentId:guid}")]
    [ProducesResponseType(typeof(ExternalPaymentDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExternalPaymentDTO>> UpdatePayment(Guid accountId, Guid paymentId, [FromBody] ExternalPaymentUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        return Ok(await _externalPaymentService.UpdatePaymentAsync(accountId, paymentId, request));
    }

    /// <summary>Deletes a pay-in or pay-out.</summary>
    [HttpDelete("{accountId:guid}/payments/{paymentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePayment(Guid accountId, Guid paymentId)
    {
        await _externalPaymentService.DeletePaymentAsync(accountId, paymentId);
        return NoContent();
    }
}
