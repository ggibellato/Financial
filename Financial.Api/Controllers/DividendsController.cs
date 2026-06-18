using Financial.Application.Configuration;
using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Financial.Api.Controllers;

[ApiController]
[Route("dividends")]
public sealed class DividendsController : ControllerBase
{
    private readonly IDividendService _dividendService;
    private readonly string _defaultExchange;

    public DividendsController(IDividendService dividendService, IOptions<DividendOptions> dividendOptions)
    {
        _dividendService = dividendService ?? throw new ArgumentNullException(nameof(dividendService));
        _defaultExchange = (dividendOptions ?? throw new ArgumentNullException(nameof(dividendOptions))).Value.DefaultExchange;
    }

    [HttpGet("{ticker}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<DividendHistoryItemDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<IReadOnlyList<DividendHistoryItemDTO>> GetDividendHistory(
        string ticker,
        [FromQuery] string? exchange = null)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            return BadRequest();
        }

        var request = BuildRequest(ticker, exchange);
        var history = _dividendService.GetDividendHistory(request);
        return Ok(history);
    }

    [HttpGet("{ticker}/summary")]
    [ProducesResponseType(typeof(DividendSummaryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<DividendSummaryDTO> GetDividendSummary(
        string ticker,
        [FromQuery] string? exchange = null)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            return BadRequest();
        }

        var request = BuildRequest(ticker, exchange);
        var summary = _dividendService.GetDividendSummary(request);
        return Ok(summary);
    }

    private DividendLookupRequestDTO BuildRequest(string ticker, string? exchange)
    {
        var resolvedExchange = string.IsNullOrWhiteSpace(exchange) ? _defaultExchange : exchange.Trim();
        return new DividendLookupRequestDTO
        {
            Exchange = resolvedExchange,
            Ticker = ticker.Trim()
        };
    }
}
