using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Financial.Investment.Application.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("navigation")]
public sealed class NavigationController : ControllerBase
{
    private readonly INavigationService _navigationService;

    public NavigationController(INavigationService navigationService)
    {
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
    }

    [HttpGet("tree")]
    [ProducesResponseType(typeof(TreeNodeDTO), StatusCodes.Status200OK)]
    public ActionResult<TreeNodeDTO> GetNavigationTree([FromQuery] string? scope)
    {
        var tree = _navigationService.GetNavigationTree(InvestmentScopeParser.ParseOrDefault(scope));
        return Ok(tree);
    }

    [HttpGet("brokers")]
    [ProducesResponseType(typeof(IEnumerable<BrokerNodeDTO>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<BrokerNodeDTO>> GetBrokers([FromQuery] string? scope)
    {
        var brokers = _navigationService.GetBrokers(InvestmentScopeParser.ParseOrDefault(scope));
        return Ok(brokers);
    }
}
