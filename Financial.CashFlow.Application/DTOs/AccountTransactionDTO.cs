namespace Financial.CashFlow.Application.DTOs;

public sealed class AccountTransactionDTO
{
    public required Guid Id { get; init; }
    public required string Kind { get; init; }
    public required DateOnly Date { get; init; }
    public string? Description { get; init; }
    public string? CategoryName { get; init; }
    public string? Counterpart { get; init; }
    public required decimal Amount { get; init; }
    public decimal? RoundUp { get; init; }
    public required bool CanEdit { get; init; }
    public AccountTransactionStatementDTO? Statement { get; init; }
    public ExpenseDTO? Expense { get; init; }
    public IncomeDTO? Income { get; init; }
    public AccountTransferDTO? Transfer { get; init; }
    public BalanceAdjustmentDTO? Adjustment { get; init; }
}
