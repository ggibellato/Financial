using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("summary")]
public sealed class SummaryController : ControllerBase
{
    private readonly ISummaryQueryService _summaryQueryService;

    public SummaryController(ISummaryQueryService summaryQueryService)
    {
        _summaryQueryService = summaryQueryService ?? throw new ArgumentNullException(nameof(summaryQueryService));
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
}
