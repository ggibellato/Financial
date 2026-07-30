using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages month-end investment account balance snapshots used for cash flow tracking.
/// </summary>
[ApiController]
[Route("investment-snapshots")]
public sealed class InvestmentSnapshotsController : ControllerBase
{
    private readonly IInvestmentSnapshotService _investmentSnapshotService;

    public InvestmentSnapshotsController(IInvestmentSnapshotService investmentSnapshotService)
    {
        _investmentSnapshotService = investmentSnapshotService ?? throw new ArgumentNullException(nameof(investmentSnapshotService));
    }

    /// <summary>Lists investment account snapshots for a given month.</summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <returns>200 OK with the matching snapshots.</returns>
    [HttpGet("{year:int}/{month:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<InvestmentSnapshotDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InvestmentSnapshotDTO>>> GetSnapshotsForMonth(int year, int month)
    {
        var result = await _investmentSnapshotService.GetSnapshotsForMonthAsync(year, month);
        return Ok(result);
    }

    /// <summary>Updates the value recorded for an investment snapshot.</summary>
    /// <param name="id">The snapshot's identifier.</param>
    /// <param name="request">The new value.</param>
    /// <returns>200 OK with the updated snapshot, 400 Bad Request if the request is invalid, or 404 Not Found if the snapshot doesn't exist.</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(InvestmentSnapshotDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvestmentSnapshotDTO>> UpdateSnapshotValue(Guid id, [FromBody] UpdateInvestmentSnapshotValueDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var result = await _investmentSnapshotService.UpdateSnapshotValueAsync(id, request);
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
