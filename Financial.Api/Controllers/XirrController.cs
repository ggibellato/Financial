using Financial.Application.DTOs;
using Financial.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("xirr")]
public sealed class XirrController : ControllerBase
{
    private readonly IXirrCalculationService _xirrCalculationService;

    public XirrController(IXirrCalculationService xirrCalculationService)
    {
        _xirrCalculationService = xirrCalculationService ?? throw new ArgumentNullException(nameof(xirrCalculationService));
    }

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
