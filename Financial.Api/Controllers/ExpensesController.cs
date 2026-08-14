using Financial.CashFlow.Application.DTOs;
using Financial.CashFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Financial.Api.Controllers;

/// <summary>
/// Manages cash flow expenses.
/// </summary>
[ApiController]
[Route("expenses")]
public sealed class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService ?? throw new ArgumentNullException(nameof(expenseService));
    }

    /// <summary>Records a new expense.</summary>
    /// <param name="request">The expense to create.</param>
    /// <returns>200 OK with the created expense, 400 Bad Request if the request is invalid or missing.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ExpenseDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ExpenseDTO>> AddExpense([FromBody] ExpenseCreateDTO? request)
    {
        if (request is null)
        {
            return BadRequest();
        }

        var expense = await _expenseService.AddExpenseAsync(request);
        return Ok(expense);
    }

    /// <summary>Updates an existing expense.</summary>
    /// <param name="id">The expense's identifier.</param>
    /// <param name="request">The new expense fields.</param>
    /// <returns>200 OK with the updated expense, 400 Bad Request if the request is invalid, or 404 Not Found if the expense doesn't exist.</returns>
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

        var expense = await _expenseService.UpdateExpenseAsync(id, request);
        return Ok(expense);
    }

    /// <summary>Deletes an expense.</summary>
    /// <param name="id">The expense's identifier.</param>
    /// <returns>200 OK if deleted, or 404 Not Found if the expense doesn't exist.</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteExpense(Guid id)
    {
        await _expenseService.DeleteExpenseAsync(id);
        return Ok();
    }

    /// <summary>Lists expenses recorded in a given month.</summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <returns>200 OK with the matching expenses.</returns>
    [HttpGet("month/{year:int}/{month:int}")]
    [ProducesResponseType(typeof(IReadOnlyList<ExpenseDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<ExpenseDTO>> GetExpensesByMonth(int year, int month)
    {
        var result = _expenseService.GetExpensesByMonth(year, month);
        return Ok(result);
    }

    /// <summary>Lists unpaid credit card charges (unsettled) recorded in a given month.</summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <returns>200 OK with the matching expenses.</returns>
    [HttpGet("month/{year:int}/{month:int}/unpaid-card-charges")]
    [ProducesResponseType(typeof(IReadOnlyList<ExpenseDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<ExpenseDTO>> GetUnpaidCardChargesByMonth(int year, int month)
    {
        var result = _expenseService.GetUnpaidCardChargesByMonth(year, month);
        return Ok(result);
    }

    /// <summary>Returns expense totals grouped by category for a given month.</summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month (1-12).</param>
    /// <returns>200 OK with the per-category totals.</returns>
    [HttpGet("month/{year:int}/{month:int}/category-totals")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryTotalDTO>), StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<CategoryTotalDTO>> GetCategoryTotalsByMonth(int year, int month)
    {
        var result = _expenseService.GetCategoryTotalsByMonth(year, month);
        return Ok(result);
    }
}
