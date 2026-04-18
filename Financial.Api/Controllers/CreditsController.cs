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

    public CreditsController(INavigationService navigationService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
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
