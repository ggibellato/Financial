using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Read-only access to the seeded income sources.
/// </summary>
[ApiController]
[Route("income-sources")]
public sealed class IncomeSourcesController : ControllerBase
{
    private readonly IIncomeSourceService _incomeSourceService;

    public IncomeSourcesController(IIncomeSourceService incomeSourceService)
    {
        _incomeSourceService = incomeSourceService ?? throw new ArgumentNullException(nameof(incomeSourceService));
    }

    /// <summary>Lists all income sources.</summary>
    /// <returns>200 OK with the full, unfiltered list of income sources.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<IncomeSourceDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<IncomeSourceDTO>> GetIncomeSources()
    {
        var result = _incomeSourceService.GetIncomeSources();
        return Ok(result);
    }
}
