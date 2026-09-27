using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Validation;
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

    private static bool TryParseOptionalCurrency(string? rawCurrency, out Currency? currency)
    {
        if (string.IsNullOrEmpty(rawCurrency))
        {
            currency = null;
            return true;
        }

        if (!EnumParser.TryParseEnum<Currency>(rawCurrency, out var parsed))
        {
            currency = null;
            return false;
        }

        currency = parsed;
        return true;
    }
}
