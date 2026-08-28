using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages recurring monthly bills ("mensais") and their paid/unpaid state.
/// </summary>
[ApiController]
[Route("mensais")]
public sealed class MensaisController : ControllerBase
{
    private readonly IMensaisService _mensaisService;

    public MensaisController(IMensaisService mensaisService)
    {
        _mensaisService = mensaisService ?? throw new ArgumentNullException(nameof(mensaisService));
    }

    /// <summary>Creates a new recurring bill.</summary>
    /// <param name="request">The bill to create.</param>
    /// <returns>200 OK with the created bill, 400 Bad Request if the request is invalid or missing.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(RecurringBillDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecurringBillDTO>> CreateBill([FromBody] RecurringBillCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var bill = await _mensaisService.CreateBillAsync(request);
        return Ok(bill);
    }

    /// <summary>Lists all recurring bills.</summary>
    /// <returns>200 OK with the list of bills.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RecurringBillDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<RecurringBillDTO>> GetBills()
    {
        return Ok(_mensaisService.GetBills());
    }

    /// <summary>Deletes a recurring bill.</summary>
    /// <param name="id">The bill's identifier.</param>
    /// <returns>200 OK if deleted, or 404 Not Found if the bill doesn't exist.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBill(Guid id)
    {
        await _mensaisService.DeleteBillAsync(id);
        return Ok();
    }

    /// <summary>Updates an existing recurring bill, such as marking it paid for the current period.</summary>
    /// <param name="id">The bill's identifier.</param>
    /// <param name="request">The new bill fields.</param>
    /// <returns>200 OK with the updated bill, 400 Bad Request if the request is invalid, or 404 Not Found if the bill doesn't exist.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RecurringBillDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecurringBillDTO>> UpdateBill(Guid id, [FromBody] RecurringBillUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var result = await _mensaisService.UpdateBillAsync(id, request);
        return Ok(result);
    }

    /// <summary>Resets every recurring bill's paid state back to unset, typically at the start of a new month.</summary>
    /// <returns>200 OK with the reset bills.</returns>
    [HttpPost("reset")]
    [ProducesResponseType(typeof(IReadOnlyList<RecurringBillDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecurringBillDTO>>> ResetAllToUnset()
    {
        var result = await _mensaisService.ResetAllToUnsetAsync();
        return Ok(result);
    }
}
