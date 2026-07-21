using Financial.Investment.Application.Configuration;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
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
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IReadOnlyList<DividendHistoryItemDTO>> GetDividendHistory(
        string ticker,
        [FromQuery] string? exchange = null)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            return BadRequest();
        }

        var request = BuildRequest(ticker, exchange);
        try
        {
            var history = _dividendService.GetDividendHistory(request);
            return Ok(history);
        }
        catch (Exception)
        {
            return DividendNotFound(ticker);
        }
    }

    [HttpGet("{ticker}/summary")]
    [ProducesResponseType(typeof(DividendSummaryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<DividendSummaryDTO> GetDividendSummary(
        string ticker,
        [FromQuery] string? exchange = null)
    {
        if (string.IsNullOrWhiteSpace(ticker))
        {
            return BadRequest();
        }

        var request = BuildRequest(ticker, exchange);
        try
        {
            var summary = _dividendService.GetDividendSummary(request);
            return Ok(summary);
        }
        catch (Exception)
        {
            return DividendNotFound(ticker);
        }
    }

    private ObjectResult DividendNotFound(string ticker) =>
        Problem(
            title: "Dividend data not found",
            detail: $"Could not find dividend data for '{ticker.Trim().ToUpperInvariant()}'. Check the ticker and try again.",
            statusCode: StatusCodes.Status404NotFound);

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
