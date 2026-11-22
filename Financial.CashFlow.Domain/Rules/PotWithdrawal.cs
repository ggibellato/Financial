using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;

namespace Financial.CashFlow.Domain.Rules;

public static class PotWithdrawal
{
    public const string BankAndCardMessage = "Choose a bank or a credit card, not both.";
    public const string StatementBeforeExpenseMessage = "The statement can't be before the expense date.";
    public const string CardWithoutCategoryMessage = "Category is required when a credit card is selected.";
    public const string RoundUpWithoutBankMessage = "Round-up only applies to a withdrawal paid from a bank.";

    public static PotOperation Build(
        Account potsAccount,
        Pot pot,
        decimal amount,
        DateOnly date,
        string description,
        Account? bank,
        Category? category,
        Guid operationId,
        DateOnly today,
        PotWithdrawalCard? card = null,
        decimal? roundUpAmount = null)
    {
        TransactionDateRule.EnsureNotInFuture(date, today);

        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.");
        }

        if (bank is not null && card is not null)
        {
            throw new ArgumentException(BankAndCardMessage);
        }

        potsAccount.EnsureOwnsPot(pot.Id);

        EnsureRoundUpOnlyWithBank(bank, roundUpAmount);

        var movement = PotTransaction.Create(pot, -amount, date, description, operationId: operationId);
        if (card is not null)
        {
            return new(operationId, potsAccount, [movement], [CardCharge(card, amount, date, description, category, operationId)]);
        }

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

        var spend = Expense.Create(date, description, amount, category, bank, creditCard: null, operationId: operationId);
        spend.SetRoundUpAmount(roundUpAmount);

        return new(operationId, potsAccount, [movement],
        [
            Expense.Create(date, description, -amount, potsCategory, bank, creditCard: null, operationId: operationId),
            spend
        ]);
    }

    public static void EnsureRoundUpOnlyWithBank(Account? bank, decimal? roundUpAmount)
    {
        if (bank is null && roundUpAmount is not null)
        {
            throw new ArgumentException(RoundUpWithoutBankMessage);
        }
    }

    public static void EnsureUsableCategory(Category category, IEnumerable<Account> accounts)
    {
        if (category.IsInvestment || accounts.Any(account => account.Category?.Id == category.Id))
        {
            throw new ArgumentException($"Category '{category.Name}' cannot be used for a withdrawal expense.");
        }
    }

    private static Expense CardCharge(
        PotWithdrawalCard card, decimal amount, DateOnly date, string description, Category? category, Guid operationId)
    {
        if (category is null)
        {
            throw new ArgumentException(CardWithoutCategoryMessage);
        }

        if (card.StatementMonth < new DateOnly(date.Year, date.Month, 1))
        {
            throw new ArgumentException(StatementBeforeExpenseMessage);
        }

        return Expense.Create(
            date, description, amount, category, paymentSourceAccount: null, creditCard: card.Card, invoiceDate: card.StatementMonth, operationId: operationId);
    }
}
