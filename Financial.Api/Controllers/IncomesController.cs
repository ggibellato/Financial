using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages cash flow incomes.
/// </summary>
[ApiController]
[Route("incomes")]
public sealed class IncomesController : ControllerBase
{
    private readonly IIncomeService _incomeService;

    public IncomesController(IIncomeService incomeService)
    {
        _incomeService = incomeService ?? throw new ArgumentNullException(nameof(incomeService));
    }

    /// <summary>Records a new income.</summary>
    /// <param name="request">The income to create.</param>
    /// <returns>200 OK with the created income, 400 Bad Request if the request is invalid or missing.</returns>
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

    /// <summary>Updates an existing income.</summary>
    /// <param name="id">The income's identifier.</param>
    /// <param name="request">The new income fields.</param>
    /// <returns>200 OK with the updated income, 400 Bad Request if the request is invalid, or 404 Not Found if the income doesn't exist.</returns>
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

    /// <summary>Deletes an income.</summary>
    /// <param name="id">The income's identifier.</param>
    /// <returns>200 OK if deleted, or 404 Not Found if the income doesn't exist.</returns>
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

    /// <summary>Lists incomes recorded in a given month.</summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <returns>200 OK with the matching incomes.</returns>
    [HttpGet("month/{year:int}/{month:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<IncomeDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<IncomeDTO>> GetIncomesByMonth(int year, int month)
    {
        var result = _incomeService.GetIncomesByMonth(year, month);
        return Ok(result);
    }
}
