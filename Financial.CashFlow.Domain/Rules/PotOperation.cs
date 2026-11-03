using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;

namespace Financial.CashFlow.Domain.Rules;

public enum PotOperationKind
{
    Deposit,
    Withdrawal
}

public sealed record PotOperation(
    Guid OperationId, Account PotsAccount, IReadOnlyList<ReserveMovement> Movements, IReadOnlyList<Expense> Expenses)
{
    public PotOperationKind Kind => Movements.Any(movement => movement.Amount < 0) ? PotOperationKind.Withdrawal : PotOperationKind.Deposit;
    public DateOnly Date => Movements[0].Date;
    public string Description => Movements[0].Description;
    public decimal Amount => Movements.Sum(movement => Math.Abs(movement.Amount));
    public ReserveBucket? Pot => Kind == PotOperationKind.Withdrawal ? Movements[0].Bucket : null;
    public Account? Bank => ChosenExpense?.PaymentSourceAccount;
    public Category? Category => ChosenExpense?.Category;
    public Guid? IncomeId => Movements.Select(movement => movement.Income?.Id).FirstOrDefault(id => id is not null);

    private Expense? ChosenExpense =>
        Kind == PotOperationKind.Withdrawal ? Expenses.FirstOrDefault(expense => expense.Value > 0) : null;

    public static PotOperation? Read(
        Guid operationId, IEnumerable<ReserveMovement> movements, IEnumerable<Expense> expenses, IEnumerable<Account> accounts)
    {
        var operationMovements = movements.Where(movement => movement.OperationId == operationId).ToList();
        var potsAccount = operationMovements.Count == 0
            ? null
            : accounts.FirstOrDefault(account => account.OwnsPot(operationMovements[0].Bucket.Id));
        if (potsAccount is null)
        {
            return null;
        }

        return new PotOperation(
            operationId, potsAccount, operationMovements, expenses.Where(expense => expense.OperationId == operationId).ToList());
    }
}
