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
    public ActionResult<TitheSummaryDTO> GetTitheSummaryByMonth(int year, int month)
    {
        var result = _titheService.GetTitheSummary(year, month);
        return Ok(result);
    }
}
