using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("dashboard")]
public sealed class DashboardController : ApiControllerBase
{
    private readonly IPortfolioDashboardService _portfolioDashboardService;

    public DashboardController(IPortfolioDashboardService portfolioDashboardService)
    {
        _portfolioDashboardService = portfolioDashboardService ?? throw new ArgumentNullException(nameof(portfolioDashboardService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(PortfolioDashboardDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PortfolioDashboardDTO>> GetPortfolioDashboard(
        [FromQuery] string? displayCurrency = null,
        [FromQuery] string? brokerCurrency = null)
    {
        if (!TryParseOptionalCurrency(displayCurrency, out var parsedDisplayCurrency) ||
            !TryParseOptionalCurrency(brokerCurrency, out var parsedBrokerCurrency))
        {
            return BadRequest();
        }

        var dto = await _portfolioDashboardService
            .GetDashboardAsync(parsedDisplayCurrency, parsedBrokerCurrency)
            .ConfigureAwait(false);
        return Ok(dto);
    }
}
