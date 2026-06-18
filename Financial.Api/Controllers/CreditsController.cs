using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("credits")]
public sealed class CreditsController : ControllerBase
{
    private readonly ICreditQueryService _creditQueryService;
    private readonly ICreditService _creditService;

    public CreditsController(ICreditQueryService creditQueryService, ICreditService creditService)
    {
        _creditQueryService = creditQueryService ?? throw new ArgumentNullException(nameof(creditQueryService));
        _creditService = creditService ?? throw new ArgumentNullException(nameof(creditService));
    }

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
        if (asset is null)
        {
            return BadRequest();
        }

        return Ok(asset);
    }

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
        if (asset is null)
        {
            return BadRequest();
        }

        return Ok(asset);
    }

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
        if (asset is null)
        {
            return BadRequest();
        }

        return Ok(asset);
    }

    [HttpGet("broker/{brokerName}")]
    [ProducesResponseType(typeof(IReadOnlyList<CreditDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<CreditDTO>> GetCreditsByBroker(string brokerName)
    {
        var credits = _creditQueryService.GetCreditsByBroker(brokerName);
        return Ok(credits);
    }

    [HttpGet("portfolio/{brokerName}/{portfolioName}")]
    [ProducesResponseType(typeof(IReadOnlyList<CreditDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<CreditDTO>> GetCreditsByPortfolio(
        string brokerName,
        string portfolioName)
    {
        var credits = _creditQueryService.GetCreditsByPortfolio(brokerName, portfolioName);
        return Ok(credits);
    }
}
