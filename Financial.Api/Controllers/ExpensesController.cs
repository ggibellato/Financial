using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

[ApiController]
[Route("expenses")]
public sealed class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ExpenseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExpenseDTO>> AddExpense([FromBody] ExpenseCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var expense = await _expenseService.AddExpenseAsync(request);
            return Ok(expense);
        }
        catch (ArgumentException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ExpenseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExpenseDTO>> UpdateExpense(Guid id, [FromBody] ExpenseUpdateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        try
        {
            var expense = await _expenseService.UpdateExpenseAsync(id, request);
            return Ok(expense);
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
    public async Task<IActionResult> DeleteExpense(Guid id)
    {
        try
        {
            await _expenseService.DeleteExpenseAsync(id);
            return Ok();
        }
        catch (KeyNotFoundException ex)
        {
            return Problem(detail: ex.Message, statusCode: StatusCodes.Status404NotFound);
        }
    }

    [HttpGet("month/{year:int}/{month:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<ExpenseDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<ExpenseDTO>> GetExpensesByMonth(int year, int month)
    {
        var result = _expenseService.GetExpensesByMonth(year, month);
        return Ok(result);
    }

    [HttpGet("month/{year:int}/{month:int}/category-totals")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryTotalDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<CategoryTotalDTO>> GetCategoryTotalsByMonth(int year, int month)
    {
        var result = _expenseService.GetCategoryTotalsByMonth(year, month);
        return Ok(result);
    }
}
