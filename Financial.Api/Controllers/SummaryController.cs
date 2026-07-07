using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("summary")]
public sealed class SummaryController : ControllerBase
{
    private readonly ISummaryQueryService _summaryQueryService;
    private readonly IPortfolioAssetSummaryQueryService _portfolioAssetSummaryQueryService;
    private readonly IBrokerBreakdownQueryService _brokerBreakdownQueryService;

    public SummaryController(
        ISummaryQueryService summaryQueryService,
        IPortfolioAssetSummaryQueryService portfolioAssetSummaryQueryService,
        IBrokerBreakdownQueryService brokerBreakdownQueryService)
    {
        _summaryQueryService = summaryQueryService ?? throw new ArgumentNullException(nameof(summaryQueryService));
        _portfolioAssetSummaryQueryService = portfolioAssetSummaryQueryService ?? throw new ArgumentNullException(nameof(portfolioAssetSummaryQueryService));
        _brokerBreakdownQueryService = brokerBreakdownQueryService ?? throw new ArgumentNullException(nameof(brokerBreakdownQueryService));
    }

    [HttpGet("broker/{brokerName}")]
    [ProducesResponseType(typeof(AggregatedSummaryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<AggregatedSummaryDTO> GetBrokerSummary(string brokerName)
    {
        var dto = _summaryQueryService.GetBrokerSummary(brokerName);
        return Ok(dto);
    }

    [HttpGet("portfolio/{brokerName}/{portfolioName}")]
    [ProducesResponseType(typeof(AggregatedSummaryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<AggregatedSummaryDTO> GetPortfolioSummary(
        string brokerName,
        string portfolioName)
    {
        var dto = _summaryQueryService.GetPortfolioSummary(brokerName, portfolioName);
        return Ok(dto);
    }

    [HttpGet("portfolio/{brokerName}/{portfolioName}/assets")]
    [ProducesResponseType(typeof(IReadOnlyList<PortfolioAssetSummaryItemDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyList<PortfolioAssetSummaryItemDTO>> GetPortfolioAssetsSummary(
        string brokerName,
        string portfolioName)
    {
        if (string.IsNullOrWhiteSpace(brokerName) || string.IsNullOrWhiteSpace(portfolioName))
            return BadRequest();

        var result = _portfolioAssetSummaryQueryService.GetPortfolioAssetsSummary(brokerName, portfolioName);
        return Ok(result);
    }

    [HttpGet("broker/{brokerName}/breakdown")]
    [ProducesResponseType(typeof(IReadOnlyList<PortfolioBreakdownItemDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyList<PortfolioBreakdownItemDTO>> GetBrokerBreakdown(string brokerName)
    {
        if (string.IsNullOrWhiteSpace(brokerName))
            return BadRequest();

        var result = _brokerBreakdownQueryService.GetBrokerBreakdown(brokerName);
        return Ok(result);
    }
}
