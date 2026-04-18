using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

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
    public ActionResult<TreeNodeDTO> GetNavigationTree()
    {
        var tree = _navigationService.GetNavigationTree();
        return Ok(tree);
    }

    [HttpGet("brokers")]
    [ProducesResponseType(typeof(IEnumerable<BrokerNodeDTO>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<BrokerNodeDTO>> GetBrokers()
    {
        var brokers = _navigationService.GetBrokers();
        return Ok(brokers);
    }
}
