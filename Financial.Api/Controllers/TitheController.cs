using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Provides tithe (charitable giving) summaries computed from income.
/// </summary>
[ApiController]
[Route("tithe")]
public sealed class TitheController : ControllerBase
{
    private readonly ITitheService _titheService;

    public TitheController(ITitheService titheService)
    {
        _titheService = titheService ?? throw new ArgumentNullException(nameof(titheService));
    }

    /// <summary>Returns the tithe summary for a given month.</summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <returns>200 OK with the tithe summary.</returns>
    [HttpGet("month/{year:int}/{month:int}")]
    [ProducesResponseType(typeof(TitheSummaryDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<TitheSummaryDTO>> GetTitheSummaryByMonth(int year, int month)
    {
        var result = await _titheService.GetTitheSummaryAsync(year, month);
        return Ok(result);
    }

    /// <summary>Includes or excludes the previous month's carried-forward amount from this month's Tithe Balance.</summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <param name="request">Whether the carry-forward should count toward this month's Tithe Balance.</param>
    /// <returns>200 OK with the updated tithe summary, or 400 Bad Request if the month is invalid or has no carry-forward available.</returns>
    [HttpPut("month/{year:int}/{month:int}/carry-forward")]
    [ProducesResponseType(typeof(TitheSummaryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TitheSummaryDTO>> UpdateCarryForwardInclusion(
        int year, int month, [FromBody] TitheCarryForwardUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var result = await _titheService.UpdateCarryForwardInclusionAsync(year, month, request.Included);
        return Ok(result);
    }
}
