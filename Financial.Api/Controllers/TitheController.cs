using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("tithe")]
public sealed class TitheController : ControllerBase
{
    private readonly ITitheService _titheService;

    public TitheController(ITitheService titheService)
    {
        _titheService = titheService ?? throw new ArgumentNullException(nameof(titheService));
    }

    [HttpGet("month/{year:int}/{month:int}")]
    [ProducesResponseType(typeof(TitheSummaryDTO), StatusCodes.Status200OK)]
    public ActionResult<TitheSummaryDTO> GetTitheSummaryByMonth(int year, int month)
    {
        var result = _titheService.GetTitheSummary(year, month);
        return Ok(result);
    }
}
