using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages investment credits (contributions not tied to a buy/sell transaction) and their query views.
/// </summary>
[ApiController]
[Route("credits")]
public sealed class CreditsController : ApiControllerBase
{
    private readonly ICreditQueryService _creditQueryService;
    private readonly ICreditService _creditService;

    public CreditsController(ICreditQueryService creditQueryService, ICreditService creditService)
    {
        _creditQueryService = creditQueryService ?? throw new ArgumentNullException(nameof(creditQueryService));
        _creditService = creditService ?? throw new ArgumentNullException(nameof(creditService));
    }

    /// <summary>Records a new investment credit.</summary>
    /// <param name="request">The credit to create.</param>
    /// <returns>200 OK with the updated asset details, or 400 Bad Request if the request is invalid.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetDetailsDTO>> AddCredit([FromBody] CreditCreateDTO request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var asset = await _creditService.AddCreditAsync(request);
        return OkOrBadRequest(asset);
    }

    /// <summary>Updates an existing investment credit.</summary>
    /// <param name="request">The credit fields to update.</param>
    /// <returns>200 OK with the updated asset details, or 400 Bad Request if the request is invalid.</returns>
    [HttpPut]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetDetailsDTO>> UpdateCredit([FromBody] CreditUpdateDTO request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var asset = await _creditService.UpdateCreditAsync(request);
        return OkOrBadRequest(asset);
    }

    /// <summary>Deletes an investment credit.</summary>
    /// <param name="request">Identifies the credit to delete.</param>
    /// <returns>200 OK with the updated asset details, or 400 Bad Request if the request is invalid.</returns>
    [HttpDelete]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AssetDetailsDTO>> DeleteCredit([FromBody] CreditDeleteDTO request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var asset = await _creditService.DeleteCreditAsync(request);
        return OkOrBadRequest(asset);
    }

    /// <summary>Lists credits for all portfolios under a broker.</summary>
    /// <param name="brokerName">The broker's name.</param>
    /// <param name="scope">Optional investment scope filter (e.g. "all", "active-only").</param>
    /// <returns>200 OK with the matching credits.</returns>
    [HttpGet("broker/{brokerName}")]
    [ProducesResponseType(typeof(IReadOnlyList<CreditDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<CreditDTO>> GetCreditsByBroker(string brokerName, [FromQuery] string? scope)
    {
        var credits = _creditQueryService.GetCreditsByBroker(brokerName, InvestmentScopeParser.ParseOrDefault(scope));
        return Ok(credits);
    }

    /// <summary>Lists credits for a specific portfolio.</summary>
    /// <param name="brokerName">The broker's name.</param>
    /// <param name="portfolioName">The portfolio's name.</param>
    /// <param name="scope">Optional investment scope filter (e.g. "all", "active-only").</param>
    /// <returns>200 OK with the matching credits.</returns>
    [HttpGet("portfolio/{brokerName}/{portfolioName}")]
    [ProducesResponseType(typeof(IReadOnlyList<CreditDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<CreditDTO>> GetCreditsByPortfolio(
        string brokerName,
        string portfolioName,
        [FromQuery] string? scope)
    {
        var credits = _creditQueryService.GetCreditsByPortfolio(brokerName, portfolioName, InvestmentScopeParser.ParseOrDefault(scope));
        return Ok(credits);
    }
}
