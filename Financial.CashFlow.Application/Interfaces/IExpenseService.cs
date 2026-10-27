using Financial.CashFlow.Application.DTOs;

namespace Financial.CashFlow.Application.Interfaces;

public interface IExpenseService
{
    Task<ExpenseDTO> AddExpenseAsync(ExpenseCreateDTO request);
    Task<ExpenseDTO> UpdateExpenseAsync(Guid id, ExpenseUpdateDTO request);
    Task DeleteExpenseAsync(Guid id);
    Task<ExpenseDTO> MoveToStatementAsync(Guid id, MoveChargeToStatementDTO request);
    IReadOnlyList<ExpenseDTO> GetExpensesByMonth(int year, int month);
    IReadOnlyList<ExpenseDTO> GetUnpaidCardChargesByMonth(int year, int month);
    IReadOnlyList<CategoryTotalDTO> GetCategoryTotalsByMonth(int year, int month);
}
