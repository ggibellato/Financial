using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages the portfolios that hold investment assets.
/// </summary>
[ApiController]
[Route("portfolios")]
public sealed class PortfoliosController : ControllerBase
{
    private readonly IPortfolioService _portfolioService;

    public PortfoliosController(IPortfolioService portfolioService)
    {
        _portfolioService = portfolioService ?? throw new ArgumentNullException(nameof(portfolioService));
    }

    /// <summary>Deletes a portfolio that holds no assets.</summary>
    /// <param name="brokerName">The broker's name.</param>
    /// <param name="portfolioName">The portfolio to delete.</param>
    /// <param name="scope">Optional investment scope filter (e.g. "all", "active-only").</param>
    /// <returns>
    /// 204 No Content when deleted,
    /// 404 Not Found if the broker or portfolio does not exist,
    /// or 409 Conflict if the portfolio still holds assets.
    /// </returns>
    [HttpDelete("{brokerName}/{portfolioName}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteEmptyPortfolio(string brokerName, string portfolioName, [FromQuery] string? scope)
    {
        await _portfolioService.DeleteEmptyPortfolioAsync(
            brokerName,
            portfolioName,
            InvestmentScopeParser.ParseOrDefault(scope));

        return NoContent();
    }
}
