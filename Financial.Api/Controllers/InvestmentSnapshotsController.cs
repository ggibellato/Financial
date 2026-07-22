using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("investment-snapshots")]
public sealed class InvestmentSnapshotsController : ControllerBase
{
    private readonly IInvestmentSnapshotService _investmentSnapshotService;

    public InvestmentSnapshotsController(IInvestmentSnapshotService investmentSnapshotService)
    {
        _investmentSnapshotService = investmentSnapshotService ?? throw new ArgumentNullException(nameof(investmentSnapshotService));
    }

    [HttpGet("{year:int}/{month:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<InvestmentSnapshotDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<InvestmentSnapshotDTO>>> GetSnapshotsForMonth(int year, int month)
    {
        var result = await _investmentSnapshotService.GetSnapshotsForMonthAsync(year, month);
        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(InvestmentSnapshotDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvestmentSnapshotDTO>> UpdateSnapshotValue(Guid id, [FromBody] UpdateInvestmentSnapshotValueDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var result = await _investmentSnapshotService.UpdateSnapshotValueAsync(id, request);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }
}
