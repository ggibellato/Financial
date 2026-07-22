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

    [HttpPost("templates")]
    [ProducesResponseType(typeof(RecurringBillTemplateDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RecurringBillTemplateDTO>> CreateTemplate([FromBody] CreateRecurringBillTemplateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var template = await _mensaisService.CreateTemplateAsync(request);
            return Ok(template);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpGet("templates")]
    [ProducesResponseType(typeof(IReadOnlyList<RecurringBillTemplateDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<RecurringBillTemplateDTO>> GetTemplates()
    {
        return Ok(_mensaisService.GetTemplates());
    }

    [HttpGet("{year:int}/{month:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<RecurringBillInstanceDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RecurringBillInstanceDTO>>> GetInstancesForMonth(int year, int month)
    {
        var result = await _mensaisService.GetInstancesForMonthAsync(year, month);
        return Ok(result);
    }

    [HttpPut("instances/{id:guid}")]
    [ProducesResponseType(typeof(RecurringBillInstanceDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RecurringBillInstanceDTO>> UpdateInstance(Guid id, [FromBody] UpdateRecurringBillInstanceDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var result = await _mensaisService.UpdateInstanceAsync(id, request);
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
}
