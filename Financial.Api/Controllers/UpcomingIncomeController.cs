using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("upcoming-income")]
public sealed class UpcomingIncomeController : ControllerBase
{
    private readonly IUpcomingIncomeService _upcomingIncomeService;

    public UpcomingIncomeController(IUpcomingIncomeService upcomingIncomeService)
    {
        _upcomingIncomeService = upcomingIncomeService ?? throw new ArgumentNullException(nameof(upcomingIncomeService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<UpcomingIncomeDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<UpcomingIncomeDTO>> GetUpcomingIncome()
    {
        return Ok(_upcomingIncomeService.GetUpcomingIncome());
    }
}
