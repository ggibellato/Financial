using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Provides aggregated investment summaries and breakdowns by broker and portfolio.
/// </summary>
[ApiController]
[Route("summary")]
public sealed class SummaryController : ControllerBase
{
    private readonly ISummaryService _summaryService;
    private readonly IPortfolioAssetSummaryService _portfolioAssetSummaryService;
    private readonly IBrokerBreakdownService _brokerBreakdownService;

    public SummaryController(
        ISummaryService summaryService,
        IPortfolioAssetSummaryService portfolioAssetSummaryService,
        IBrokerBreakdownService brokerBreakdownService)
    {
        _summaryService = summaryService ?? throw new ArgumentNullException(nameof(summaryService));
        _portfolioAssetSummaryService = portfolioAssetSummaryService ?? throw new ArgumentNullException(nameof(portfolioAssetSummaryService));
        _brokerBreakdownService = brokerBreakdownService ?? throw new ArgumentNullException(nameof(brokerBreakdownService));
    }

    /// <summary>Returns the aggregated investment summary for a broker.</summary>
    /// <param name="brokerName">The broker's name.</param>
    /// <param name="scope">Optional investment scope filter (e.g. "all", "active-only").</param>
    /// <returns>200 OK with the aggregated summary.</returns>
    [HttpGet("broker/{brokerName}")]
    [ProducesResponseType(typeof(AggregatedSummaryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AggregatedSummaryDTO>> GetBrokerSummary(string brokerName, [FromQuery] string? scope)
    {
        var dto = await _summaryService.GetBrokerSummaryAsync(brokerName, InvestmentScopeParser.ParseOrDefault(scope)).ConfigureAwait(false);
        return Ok(dto);
    }

    /// <summary>Returns the aggregated investment summary for a single portfolio.</summary>
    /// <param name="brokerName">The broker's name.</param>
    /// <param name="portfolioName">The portfolio's name.</param>
    /// <param name="scope">Optional investment scope filter (e.g. "all", "active-only").</param>
    /// <returns>200 OK with the aggregated summary.</returns>
    [HttpGet("portfolio/{brokerName}/{portfolioName}")]
    [ProducesResponseType(typeof(AggregatedSummaryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AggregatedSummaryDTO>> GetPortfolioSummary(
        string brokerName,
        string portfolioName,
        [FromQuery] string? scope)
    {
        var dto = await _summaryService.GetPortfolioSummaryAsync(brokerName, portfolioName, InvestmentScopeParser.ParseOrDefault(scope)).ConfigureAwait(false);
        return Ok(dto);
    }

    /// <summary>Lists per-asset summaries for a portfolio.</summary>
    /// <param name="brokerName">The broker's name.</param>
    /// <param name="portfolioName">The portfolio's name.</param>
    /// <param name="scope">Optional investment scope filter (e.g. "all", "active-only").</param>
    /// <returns>200 OK with the per-asset summaries, or 400 Bad Request if the broker or portfolio name is missing.</returns>
    [HttpGet("portfolio/{brokerName}/{portfolioName}/assets")]
    [ProducesResponseType(typeof(IReadOnlyList<PortfolioAssetSummaryItemDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyList<PortfolioAssetSummaryItemDTO>> GetPortfolioAssetsSummary(
        string brokerName,
        string portfolioName,
        [FromQuery] string? scope)
    {
        var result = _portfolioAssetSummaryService.GetPortfolioAssetsSummary(brokerName, portfolioName, InvestmentScopeParser.ParseOrDefault(scope));
        return Ok(result);
    }

    /// <summary>Lists a broker's active holdings grouped by portfolio, with subtotals and a broker total.</summary>
    /// <param name="brokerName">The broker's name.</param>
    /// <returns>200 OK with the grouped holdings (an unknown broker returns no groups), or 400 Bad Request if <paramref name="brokerName"/> is missing.</returns>
    [HttpGet("broker/{brokerName}/holdings")]
    [ProducesResponseType(typeof(BrokerHoldingsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<BrokerHoldingsDTO> GetBrokerHoldings(string brokerName)
    {
        return Ok(_portfolioAssetSummaryService.GetBrokerHoldings(brokerName));
    }

    /// <summary>Returns a broker's ARCA allocation: each assigned bucket's totals and share of ARCA, the ARCA total, the Reserve fund and the portfolios outside ARCA.</summary>
    /// <param name="brokerName">The broker's name.</param>
    /// <returns>200 OK with the held currencies and the allocation (null when no held portfolio has an ARCA bucket), or 400 Bad Request if <paramref name="brokerName"/> is missing.</returns>
    [HttpGet("broker/{brokerName}/arca")]
    [ProducesResponseType(typeof(BrokerArcaDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<BrokerArcaDTO> GetBrokerArca(string brokerName)
    {
        return Ok(_portfolioAssetSummaryService.GetBrokerArca(brokerName));
    }

    /// <summary>Lists the portfolio breakdown (allocation by portfolio) for a broker.</summary>
    /// <param name="brokerName">The broker's name.</param>
    /// <param name="scope">Optional investment scope filter (e.g. "all", "active-only").</param>
    /// <returns>200 OK with the portfolio breakdown, or 400 Bad Request if <paramref name="brokerName"/> is missing.</returns>
    [HttpGet("broker/{brokerName}/breakdown")]
    [ProducesResponseType(typeof(IReadOnlyList<PortfolioBreakdownItemDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyList<PortfolioBreakdownItemDTO>> GetBrokerBreakdown(string brokerName, [FromQuery] string? scope)
    {
        var result = _brokerBreakdownService.GetBrokerBreakdown(brokerName, InvestmentScopeParser.ParseOrDefault(scope));
        return Ok(result);
    }
}
