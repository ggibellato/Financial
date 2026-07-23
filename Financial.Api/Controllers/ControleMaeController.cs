using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("controle-mae")]
public sealed class ControleMaeController : ControllerBase
{
    private readonly IControleMaeService _controleMaeService;

    public ControleMaeController(IControleMaeService controleMaeService)
    {
        _controleMaeService = controleMaeService ?? throw new ArgumentNullException(nameof(controleMaeService));
    }

    [HttpPost("entries")]
    [ProducesResponseType(typeof(MaeLedgerEntryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MaeLedgerEntryDTO>> CreateEntry([FromBody] CreateMaeLedgerEntryDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var entry = await _controleMaeService.CreateEntryAsync(request);
            return Ok(entry);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpGet("entries/from/{fromDate}")]
    [ProducesResponseType(typeof(IReadOnlyList<MaeLedgerEntryDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<MaeLedgerEntryDTO>> GetEntriesFromDate(DateOnly fromDate)
    {
        return Ok(_controleMaeService.GetEntriesFromDate(fromDate));
    }

    [HttpGet("entries/totals")]
    [ProducesResponseType(typeof(MaeLedgerTotalsDTO), StatusCodes.Status200OK)]
    public ActionResult<MaeLedgerTotalsDTO> GetTotals()
    {
        return Ok(_controleMaeService.GetTotals());
    }

    [HttpPut("entries/{id:guid}/values")]
    [ProducesResponseType(typeof(MaeLedgerEntryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaeLedgerEntryDTO>> UpdateEntryValues(Guid id, [FromBody] UpdateMaeLedgerEntryValuesDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var result = await _controleMaeService.UpdateEntryValuesAsync(id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    [HttpDelete("entries/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEntry(Guid id)
    {
        try
        {
            await _controleMaeService.DeleteEntryAsync(id);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }
}
