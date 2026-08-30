using Financial.Investment.Application.DTOs;
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

    /// <summary>Lists every portfolio across both Active and Historic brokers.</summary>
    /// <returns>200 OK with the list of portfolios.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PortfolioDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<PortfolioDTO>> GetPortfolios()
    {
        return Ok(_portfolioService.GetPortfolios());
    }

    /// <summary>Registers a new portfolio under an Active broker.</summary>
    /// <param name="request">The parent broker's name and the portfolio's name.</param>
    /// <returns>200 OK with the created portfolio, 400 Bad Request if invalid, 404 Not Found if the broker doesn't exist or isn't Active, or 409 Conflict if the name is already in use under that broker.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(PortfolioDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PortfolioDTO>> CreatePortfolio([FromBody] PortfolioCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var portfolio = await _portfolioService.CreatePortfolioAsync(request);
        return Ok(portfolio);
    }

    /// <summary>Renames an existing portfolio. The parent broker is fixed and not part of this operation.</summary>
    /// <param name="brokerName">The portfolio's parent broker name.</param>
    /// <param name="portfolioName">The portfolio's current name.</param>
    /// <param name="request">The portfolio's new name.</param>
    /// <returns>200 OK with the updated portfolio, 400 Bad Request if invalid, 404 Not Found if the broker or portfolio doesn't exist, or 409 Conflict if the new name is already in use under that broker.</returns>
    [HttpPut("{brokerName}/{portfolioName}")]
    [ProducesResponseType(typeof(PortfolioDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PortfolioDTO>> UpdatePortfolio(string brokerName, string portfolioName, [FromBody] PortfolioUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var portfolio = await _portfolioService.UpdatePortfolioAsync(brokerName, portfolioName, request);
        return Ok(portfolio);
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
