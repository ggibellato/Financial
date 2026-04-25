using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace Financial.Api.Controllers;

[ApiController]
[Route("credits")]
public sealed class CreditsController : ControllerBase
{
    private readonly INavigationService _navigationService;
    private readonly ICreditService _creditService;

    public CreditsController(INavigationService navigationService, ICreditService creditService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _creditService = creditService ?? throw new ArgumentNullException(nameof(creditService));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<AssetDetailsDTO> AddCredit([FromBody] CreditCreateDTO request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var asset = _creditService.AddCredit(request);
        if (asset is null)
        {
            return BadRequest();
        }

        return Ok(asset);
    }

    [HttpPut]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<AssetDetailsDTO> UpdateCredit([FromBody] CreditUpdateDTO request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var asset = _creditService.UpdateCredit(request);
        if (asset is null)
        {
            return BadRequest();
        }

        return Ok(asset);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(AssetDetailsDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<AssetDetailsDTO> DeleteCredit([FromBody] CreditDeleteDTO request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var asset = _creditService.DeleteCredit(request);
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
        var credits = _navigationService.GetCreditsByBroker(brokerName);
        return Ok(credits);
    }

    [HttpGet("portfolio/{brokerName}/{portfolioName}")]
    [ProducesResponseType(typeof(IReadOnlyList<CreditDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<CreditDTO>> GetCreditsByPortfolio(
        string brokerName,
        string portfolioName)
    {
        var credits = _navigationService.GetCreditsByPortfolio(brokerName, portfolioName);
        return Ok(credits);
    }
}
