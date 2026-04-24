using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace Financial.Api.Controllers;

[ApiController]
[Route("dividends")]
public sealed class DividendsController : ControllerBase
{
    private const string DefaultExchange = "BVMF";
    private readonly IDividendService _dividendService;

    public DividendsController(IDividendService dividendService)
    {
        _dividendService = dividendService ?? throw new ArgumentNullException(nameof(dividendService));
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

    private static DividendLookupRequestDTO BuildRequest(string ticker, string? exchange)
    {
        var resolvedExchange = string.IsNullOrWhiteSpace(exchange) ? DefaultExchange : exchange.Trim();
        return new DividendLookupRequestDTO
        {
            Exchange = resolvedExchange,
            Ticker = ticker.Trim()
        };
    }
}
