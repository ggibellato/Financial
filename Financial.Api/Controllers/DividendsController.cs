using Financial.Investment.Application.Configuration;
using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Exceptions;
using Financial.Investment.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Financial.Api.Controllers;

/// <summary>
/// Provides dividend history and summary lookups for a given ticker.
/// </summary>
[ApiController]
[Route("dividends")]
public sealed class DividendsController : ControllerBase
{
    private readonly IDividendService _dividendService;
    private readonly string _defaultExchange;
    private readonly ILogger<DividendsController> _logger;

    public DividendsController(IDividendService dividendService, IOptions<DividendOptions> dividendOptions, ILogger<DividendsController> logger)
    {
        _dividendService = dividendService ?? throw new ArgumentNullException(nameof(dividendService));
        _defaultExchange = (dividendOptions ?? throw new ArgumentNullException(nameof(dividendOptions))).Value.DefaultExchange;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>Returns the historical dividend payments for a ticker.</summary>
    /// <param name="ticker">The stock/ETF ticker symbol.</param>
    /// <param name="exchange">Optional exchange code; defaults to the configured default exchange.</param>
    /// <returns>200 OK with the dividend history, 400 Bad Request if <paramref name="ticker"/> is missing, or 404 Not Found if no dividend data exists for the ticker.</returns>
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
        catch (Exception ex)
        {
            // error.type only, plus the public ticker symbol - never the provider's message (FR-014).
            _logger.LogWarning("Dividend history lookup for ticker {Ticker} failed with {ErrorType}; returning 404", ticker, ex.GetType().Name);
            throw new DividendNotFoundException(NotFoundMessage(ticker));
        }
    }

    /// <summary>Returns a summarized view of dividend payments for a ticker (e.g. yearly totals).</summary>
    /// <param name="ticker">The stock/ETF ticker symbol.</param>
    /// <param name="exchange">Optional exchange code; defaults to the configured default exchange.</param>
    /// <returns>200 OK with the dividend summary, 400 Bad Request if <paramref name="ticker"/> is missing, or 404 Not Found if no dividend data exists for the ticker.</returns>
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
        catch (Exception ex)
        {
            _logger.LogWarning("Dividend summary lookup for ticker {Ticker} failed with {ErrorType}; returning 404", ticker, ex.GetType().Name);
            throw new DividendNotFoundException(NotFoundMessage(ticker));
        }
    }

    private static string NotFoundMessage(string ticker) =>
        $"Could not find dividend data for '{ticker.Trim().ToUpperInvariant()}'. Check the ticker and try again.";

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
