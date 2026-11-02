using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;

namespace Financial.CashFlow.Domain.Rules;

public sealed record PotWithdrawalRecords(ReserveMovement Movement, IReadOnlyList<Expense> BankExpenses);

public static class PotWithdrawal
{
    public static PotWithdrawalRecords Build(
        Account potsAccount,
        ReserveBucket pot,
        decimal amount,
        DateOnly date,
        string description,
        Account? bank,
        Category? category,
        Guid operationId)
    {
        if (!potsAccount.OwnsPot(pot.Id))
        {
            throw new ArgumentException($"That pot isn't in {potsAccount.Name}.");
        }

        var movement = ReserveMovement.Create(pot, -amount, date, description, operationId: operationId);
        if (bank is null)
        {
            return new(movement, []);
        }

        ArgumentNullException.ThrowIfNull(category);
        var potsCategory = potsAccount.Category
            ?? throw new ArgumentException($"Set a category on {potsAccount.Name} first.");

        return new(movement,
        [
            Expense.Create(date, description, -amount, potsCategory, bank, creditCard: null, operationId: operationId),
            Expense.Create(date, description, amount, category, bank, creditCard: null, operationId: operationId)
        ]);
    }

    public static void EnsureUsableCategory(Category category, IEnumerable<Account> accounts)
    {
        if (category.IsInvestment || accounts.Any(account => account.Category?.Id == category.Id))
        {
            throw new ArgumentException($"Category '{category.Name}' cannot be used for a withdrawal expense.");
        }
    }
}
