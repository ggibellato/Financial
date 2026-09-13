using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Shared.Abstractions.Currencies;
using Financial.Shared.Abstractions.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Reads and writes the reporting-currency setting used to convert portfolio- and broker-level totals.
/// </summary>
[ApiController]
[Route("reporting-currency")]
public sealed class ReportingCurrencyController : ControllerBase
{
    private readonly IReportingCurrencyProvider _reportingCurrencyProvider;

    public ReportingCurrencyController(IReportingCurrencyProvider reportingCurrencyProvider)
    {
        _reportingCurrencyProvider = reportingCurrencyProvider ?? throw new ArgumentNullException(nameof(reportingCurrencyProvider));
    }

    /// <summary>Returns the current reporting-currency setting.</summary>
    /// <returns>200 OK with the current setting.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ReportingCurrencySettingDTO), StatusCodes.Status200OK)]
    public ActionResult<ReportingCurrencySettingDTO> GetReportingCurrency() =>
        Ok(ToDto(_reportingCurrencyProvider.GetReportingCurrency()));

    /// <summary>Changes the reporting-currency setting.</summary>
    /// <param name="request">The new currency.</param>
    /// <returns>200 OK with the updated setting, or 400 Bad Request if the currency is missing or unrecognized.</returns>
    [HttpPut]
    [ProducesResponseType(typeof(ReportingCurrencySettingDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReportingCurrencySettingDTO>> SetReportingCurrency([FromBody] ReportingCurrencySettingDTO? request)
    {
        if (request is null || !EnumParser.TryParseEnum<Currency>(request.Currency, out var currency))
        {
            return BadRequest();
        }

        await _reportingCurrencyProvider.SetReportingCurrencyAsync(currency).ConfigureAwait(false);
        return Ok(ToDto(currency));
    }

    private static ReportingCurrencySettingDTO ToDto(Currency currency) => new() { Currency = currency.ToString() };
}
