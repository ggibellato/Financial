using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Exposes the whole-portfolio investment aggregate behind the Dashboard page.
/// </summary>
[ApiController]
[Route("dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IPortfolioDashboardService _portfolioDashboardService;

    public DashboardController(IPortfolioDashboardService portfolioDashboardService)
    {
        _portfolioDashboardService = portfolioDashboardService ?? throw new ArgumentNullException(nameof(portfolioDashboardService));
    }

    /// <summary>Returns the portfolio-wide aggregate across every Active and Historic broker.</summary>
    /// <returns>200 OK with the portfolio dashboard aggregate.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PortfolioDashboardDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<PortfolioDashboardDTO>> GetPortfolioDashboard()
    {
        var dto = await _portfolioDashboardService.GetDashboardAsync().ConfigureAwait(false);
        return Ok(dto);
    }
}
