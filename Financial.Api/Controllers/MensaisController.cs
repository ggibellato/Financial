using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("mensais")]
public sealed class MensaisController : ControllerBase
{
    private readonly IMensaisService _mensaisService;

    public MensaisController(IMensaisService mensaisService)
    {
        _mensaisService = mensaisService ?? throw new ArgumentNullException(nameof(mensaisService));
    }

    [HttpPost]
    [ProducesResponseType(typeof(RecurringBillDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecurringBillDTO>> CreateBill([FromBody] CreateRecurringBillDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var bill = await _mensaisService.CreateBillAsync(request);
            return Ok(bill);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<RecurringBillDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<RecurringBillDTO>> GetBills()
    {
        return Ok(_mensaisService.GetBills());
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBill(Guid id)
    {
        try
        {
            await _mensaisService.DeleteBillAsync(id);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(RecurringBillDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecurringBillDTO>> UpdateBill(Guid id, [FromBody] UpdateRecurringBillDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var result = await _mensaisService.UpdateBillAsync(id, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    [HttpPost("reset")]
    [ProducesResponseType(typeof(IReadOnlyList<RecurringBillDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecurringBillDTO>>> ResetAllToUnset()
    {
        var result = await _mensaisService.ResetAllToUnsetAsync();
        return Ok(result);
    }
}
