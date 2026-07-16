using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Financial.Application.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

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

    [HttpGet("broker/{brokerName}")]
    [ProducesResponseType(typeof(AggregatedSummaryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<AggregatedSummaryDTO> GetBrokerSummary(string brokerName, [FromQuery] string? scope)
    {
        var dto = _summaryService.GetBrokerSummary(brokerName, InvestmentScopeParser.ParseOrDefault(scope));
        return Ok(dto);
    }

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
