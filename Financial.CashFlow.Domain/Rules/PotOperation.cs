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
    Guid OperationId, Account PotsAccount, IReadOnlyList<PotTransaction> Transactions, IReadOnlyList<Expense> Expenses)
{
    public PotOperationKind Kind => Transactions.Any(transaction => transaction.Amount < 0) ? PotOperationKind.Withdrawal : PotOperationKind.Deposit;
    public DateOnly Date => Transactions[0].Date;
    public string Description => Transactions[0].Description;
    public decimal Amount => Transactions.Sum(transaction => Math.Abs(transaction.Amount));
    public PotDepositTarget? Into => (Kind, Transactions.Count) switch
    {
        (PotOperationKind.Withdrawal, _) => null,
        (_, 1) => PotDepositTarget.OnePot,
        _ => PotDepositTarget.ActivePots
    };
    public Pot? Pot => Into == PotDepositTarget.ActivePots ? null : Transactions[0].Pot;
    public IReadOnlyList<PotFundingLine> FundingLines => Kind == PotOperationKind.Deposit
        ? Expenses
            .Where(expense => expense.Category.Id == PotsAccount.Category?.Id)
            .Select(expense => new PotFundingLine(expense.PaymentSourceAccount!, expense.Value))
            .ToList()
        : [];
    public Account? Bank => ChosenExpense?.PaymentSourceAccount;
    public Category? Category => ChosenExpense?.Category;
    public Guid? IncomeId => Transactions.Select(transaction => transaction.Income?.Id).FirstOrDefault(id => id is not null);

    private Expense? ChosenExpense =>
        Kind == PotOperationKind.Withdrawal ? Expenses.FirstOrDefault(expense => expense.Value > 0) : null;

    public static PotOperation? Read(
        Guid operationId, IEnumerable<PotTransaction> transactions, IEnumerable<Expense> expenses, IEnumerable<Account> accounts)
    {
        var operationTransactions = transactions.Where(transaction => transaction.OperationId == operationId).ToList();
        var potsAccount = operationTransactions.Count == 0 ? null : operationTransactions[0].OwningAccount(accounts);
        if (potsAccount is null)
        {
            return null;
        }

        return new PotOperation(
            operationId, potsAccount, operationTransactions, expenses.Where(expense => expense.OperationId == operationId).ToList());
    }
}
