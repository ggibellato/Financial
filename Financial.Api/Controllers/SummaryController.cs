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
    public ActionResult<AggregatedSummaryDTO> GetBrokerSummary(string brokerName, [FromQuery] string? scope)
    {
        var dto = _summaryService.GetBrokerSummary(brokerName, InvestmentScopeParser.ParseOrDefault(scope));
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
    public ActionResult<AggregatedSummaryDTO> GetPortfolioSummary(
        string brokerName,
        string portfolioName,
        [FromQuery] string? scope)
    {
        var dto = _summaryService.GetPortfolioSummary(brokerName, portfolioName, InvestmentScopeParser.ParseOrDefault(scope));
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
        if (string.IsNullOrWhiteSpace(brokerName) || string.IsNullOrWhiteSpace(portfolioName))
            return BadRequest();

        var result = _portfolioAssetSummaryService.GetPortfolioAssetsSummary(brokerName, portfolioName, InvestmentScopeParser.ParseOrDefault(scope));
        return Ok(result);
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
        if (string.IsNullOrWhiteSpace(brokerName))
            return BadRequest();

        var result = _brokerBreakdownService.GetBrokerBreakdown(brokerName, InvestmentScopeParser.ParseOrDefault(scope));
        return Ok(result);
    }
}
