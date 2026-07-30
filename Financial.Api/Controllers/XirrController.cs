using Financial.Investment.Application.DTOs;
using Financial.Investment.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Calculates the extended internal rate of return (XIRR) for a set of cash flows.
/// </summary>
[ApiController]
[Route("xirr")]
public sealed class XirrController : ControllerBase
{
    private readonly IXirrCalculationService _xirrCalculationService;

    public XirrController(IXirrCalculationService xirrCalculationService)
    {
        _xirrCalculationService = xirrCalculationService ?? throw new ArgumentNullException(nameof(xirrCalculationService));
    }

    /// <summary>Calculates the XIRR for the given cash flows and terminal value.</summary>
    /// <param name="request">The dated cash flows and terminal value to calculate against.</param>
    /// <returns>200 OK with the calculated XIRR, or 400 Bad Request if the request body is missing.</returns>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(XirrResultDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<XirrResultDTO> Calculate([FromBody] CalculateXirrRequestDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var xirr = _xirrCalculationService.Calculate(request.CashFlows, request.TerminalValue);

        return Ok(new XirrResultDTO { Xirr = xirr });
    }
}
