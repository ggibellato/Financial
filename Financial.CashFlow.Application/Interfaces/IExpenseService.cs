using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IExpenseService
{
    Task<ExpenseDTO> AddExpenseAsync(ExpenseCreateDTO request);
    Task<ExpenseDTO> UpdateExpenseAsync(Guid id, ExpenseUpdateDTO request);
    Task DeleteExpenseAsync(Guid id);
    Task<ExpenseDTO> MoveToStatementAsync(Guid id, MoveChargeToStatementDTO request);
    IReadOnlyList<CategoryTotalDTO> GetCategoryTotalsByMonth(int year, int month);
}
