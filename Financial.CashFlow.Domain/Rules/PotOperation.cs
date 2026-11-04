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

public enum PotDepositTarget
{
    ActivePots,
    OnePot
}

public sealed record PotOperation(
    Guid OperationId, Account PotsAccount, IReadOnlyList<PotMovement> Movements, IReadOnlyList<Expense> Expenses)
{
    public PotOperationKind Kind => Movements.Any(movement => movement.Amount < 0) ? PotOperationKind.Withdrawal : PotOperationKind.Deposit;
    public DateOnly Date => Movements[0].Date;
    public string Description => Movements[0].Description;
    public decimal Amount => Movements.Sum(movement => Math.Abs(movement.Amount));
    public PotDepositTarget? Into => (Kind, Movements.Count) switch
    {
        (PotOperationKind.Withdrawal, _) => null,
        (_, 1) => PotDepositTarget.OnePot,
        _ => PotDepositTarget.ActivePots
    };
    public Pot? Pot => Into == PotDepositTarget.ActivePots ? null : Movements[0].Pot;
    public IReadOnlyList<PotFundingLine> FundingLines => Kind == PotOperationKind.Deposit
        ? Expenses
            .Where(expense => expense.Category.Id == PotsAccount.Category?.Id)
            .Select(expense => new PotFundingLine(expense.PaymentSourceAccount!, expense.Value))
            .ToList()
        : [];
    public Account? Bank => ChosenExpense?.PaymentSourceAccount;
    public Category? Category => ChosenExpense?.Category;
    public Guid? IncomeId => Movements.Select(movement => movement.Income?.Id).FirstOrDefault(id => id is not null);

    private Expense? ChosenExpense =>
        Kind == PotOperationKind.Withdrawal ? Expenses.FirstOrDefault(expense => expense.Value > 0) : null;

    public static PotOperation? Read(
        Guid operationId, IEnumerable<PotMovement> movements, IEnumerable<Expense> expenses, IEnumerable<Account> accounts)
    {
        var operationMovements = movements.Where(movement => movement.OperationId == operationId).ToList();
        var potsAccount = operationMovements.Count == 0
            ? null
            : accounts.FirstOrDefault(account => account.OwnsPot(operationMovements[0].Pot.Id));
        if (potsAccount is null)
        {
            return null;
        }

        return new PotOperation(
            operationId, potsAccount, operationMovements, expenses.Where(expense => expense.OperationId == operationId).ToList());
    }
}
