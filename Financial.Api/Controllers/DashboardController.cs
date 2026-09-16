using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IPortfolioDashboardService _portfolioDashboardService;

    public DashboardController(IPortfolioDashboardService portfolioDashboardService)
    {
        _portfolioDashboardService = portfolioDashboardService ?? throw new ArgumentNullException(nameof(portfolioDashboardService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(PortfolioDashboardDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<PortfolioDashboardDTO>> GetPortfolioDashboard()
    {
        var dto = await _portfolioDashboardService.GetDashboardAsync().ConfigureAwait(false);
        return Ok(dto);
    }
}
