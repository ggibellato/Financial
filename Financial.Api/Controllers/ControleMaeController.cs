using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages "Controle Mae" ledger entries — a custom cash flow ledger used to track a specific set of transfers.
/// </summary>
[ApiController]
[Route("controle-mae")]
public sealed class ControleMaeController : ControllerBase
{
    private readonly IControleMaeService _controleMaeService;

    public ControleMaeController(IControleMaeService controleMaeService)
    {
        _controleMaeService = controleMaeService ?? throw new ArgumentNullException(nameof(controleMaeService));
    }

    /// <summary>Creates a new ledger entry.</summary>
    /// <param name="request">The entry to create.</param>
    /// <returns>200 OK with the created entry, or 400 Bad Request if the request is invalid.</returns>
    [HttpPost("entries")]
    [ProducesResponseType(typeof(MaeLedgerEntryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MaeLedgerEntryDTO>> CreateEntry([FromBody] MaeLedgerEntryCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var entry = await _controleMaeService.CreateEntryAsync(request);
        return Ok(entry);
    }

    /// <summary>Lists all ledger entries recorded on or after the given date.</summary>
    /// <param name="fromDate">The earliest entry date to include.</param>
    /// <returns>200 OK with the matching entries.</returns>
    [HttpGet("entries/from/{fromDate}")]
    [ProducesResponseType(typeof(IReadOnlyList<MaeLedgerEntryDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<MaeLedgerEntryDTO>> GetEntriesFromDate(DateOnly fromDate)
    {
        return Ok(_controleMaeService.GetEntriesFromDate(fromDate));
    }

    /// <summary>Returns the running totals across all ledger entries.</summary>
    /// <returns>200 OK with the ledger totals.</returns>
    [HttpGet("entries/totals")]
    [ProducesResponseType(typeof(MaeLedgerTotalsDTO), StatusCodes.Status200OK)]
    public ActionResult<MaeLedgerTotalsDTO> GetTotals()
    {
        return Ok(_controleMaeService.GetTotals());
    }

    /// <summary>Updates the values of an existing ledger entry.</summary>
    /// <param name="id">The entry's identifier.</param>
    /// <param name="request">The new values.</param>
    /// <returns>200 OK with the updated entry, 400 Bad Request if the request body is missing, or 404 Not Found if the entry doesn't exist.</returns>
    [HttpPut("entries/{id:guid}/values")]
    [ProducesResponseType(typeof(MaeLedgerEntryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MaeLedgerEntryDTO>> UpdateEntryValues(Guid id, [FromBody] MaeLedgerEntryValuesUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var result = await _controleMaeService.UpdateEntryValuesAsync(id, request);
        return Ok(result);
    }

    /// <summary>Deletes a ledger entry.</summary>
    /// <param name="id">The entry's identifier.</param>
    /// <returns>200 OK if deleted, or 404 Not Found if the entry doesn't exist.</returns>
    [HttpDelete("entries/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteEntry(Guid id)
    {
        await _controleMaeService.DeleteEntryAsync(id);
        return Ok();
    }
}
