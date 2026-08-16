using Financial.CashFlow.Domain.Entities;

namespace Financial.CashFlow.Application.Validation;

public static class ExpenseValidationRules
{
    public const decimal MinRoundUpAmount = Expense.MinRoundUpAmount;
    public const decimal MaxRoundUpAmount = Expense.MaxRoundUpAmount;
}
