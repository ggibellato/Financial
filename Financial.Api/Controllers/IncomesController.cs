using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("incomes")]
public sealed class IncomesController : ControllerBase
{
    private readonly IIncomeService _incomeService;

    public IncomesController(IIncomeService incomeService)
    {
        _incomeService = incomeService ?? throw new ArgumentNullException(nameof(incomeService));
    }

    [HttpPost]
    [ProducesResponseType(typeof(IncomeDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IncomeDTO>> AddIncome([FromBody] IncomeCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var income = await _incomeService.AddIncomeAsync(request);
            return Ok(income);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(IncomeDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IncomeDTO>> UpdateIncome(Guid id, [FromBody] IncomeUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var income = await _incomeService.UpdateIncomeAsync(id, request);
            return Ok(income);
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

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteIncome(Guid id)
    {
        try
        {
            await _incomeService.DeleteIncomeAsync(id);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    [HttpGet("month/{year:int}/{month:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<IncomeDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<IncomeDTO>> GetIncomesByMonth(int year, int month)
    {
        var result = _incomeService.GetIncomesByMonth(year, month);
        return Ok(result);
    }
}
