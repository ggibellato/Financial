using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;

namespace Financial.CashFlow.Domain.Rules;

public static class PotWithdrawal
{
    public static PotOperation Build(
        Account potsAccount,
        ReserveBucket pot,
        decimal amount,
        DateOnly date,
        string description,
        Account? bank,
        Category? category,
        Guid operationId)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.");
        }

        if (!potsAccount.OwnsPot(pot.Id))
        {
            throw new ArgumentException($"That pot isn't in {potsAccount.Name}.");
        }

        var movement = ReserveMovement.Create(pot, -amount, date, description, operationId: operationId);
        if (bank is null)
        {
            return new(operationId, potsAccount, [movement], []);
        }

        if (category is null)
        {
            throw new ArgumentException("Category is required when a bank is selected.");
        }

        var potsCategory = potsAccount.Category
            ?? throw new ArgumentException($"Set a category on {potsAccount.Name} first.");

        return new(operationId, potsAccount, [movement],
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
