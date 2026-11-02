using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;

namespace Financial.CashFlow.Domain.Rules;

public sealed record PotOperation(
    Guid OperationId,
    bool IsWithdrawal,
    Account PotsAccount,
    DateOnly Date,
    string Description,
    decimal Amount,
    ReserveBucket? Pot,
    Account? Bank,
    Category? Category,
    Guid? IncomeId,
    IReadOnlyList<ReserveMovement> Movements,
    IReadOnlyList<Expense> Expenses)
{
    public static PotOperation? Read(
        Guid operationId, IEnumerable<ReserveMovement> movements, IEnumerable<Expense> expenses, IEnumerable<Account> accounts)
    {
        var operationMovements = movements.Where(movement => movement.OperationId == operationId).ToList();
        if (operationMovements.Count == 0)
        {
            return null;
        }

        var first = operationMovements[0];
        var potsAccount = accounts.First(account => account.OwnsPot(first.Bucket.Id));
        var operationExpenses = expenses.Where(expense => expense.OperationId == operationId).ToList();
        var isWithdrawal = operationMovements.Any(movement => movement.Amount < 0);
        var chosenExpense = isWithdrawal ? operationExpenses.FirstOrDefault(expense => expense.Value > 0) : null;

        return new PotOperation(
            operationId,
            isWithdrawal,
            potsAccount,
            first.Date,
            first.Description,
            operationMovements.Sum(movement => Math.Abs(movement.Amount)),
            isWithdrawal ? first.Bucket : null,
            chosenExpense?.PaymentSourceAccount,
            chosenExpense?.Category,
            operationMovements.Select(movement => movement.Income?.Id).FirstOrDefault(id => id is not null),
            operationMovements,
            operationExpenses);
    }
}
