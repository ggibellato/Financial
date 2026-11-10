using System;
using System.Collections.Generic;
using System.Linq;
using Financial.CashFlow.Domain.Entities;
using Financial.CashFlow.Domain.Entities.Transactions;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.CashFlow.Domain.Rules;

public sealed record PotFundingLine(Account Bank, decimal Amount);

public static class PotDeposit
{
    public const int MaxFundingLines = 5;

    public static PotOperation Build(
        Account potsAccount,
        decimal amount,
        DateOnly date,
        string description,
        Pot? pot,
        IReadOnlyList<PotFundingLine> fundingLines,
        Guid operationId) =>
        Build(potsAccount, amount, date, description, pot, fundingLines, operationId, income: null, $"Deposit to {potsAccount.Name}");

    public static PotOperation BuildFromIncome(
        Income income, Account potsAccount, IReadOnlyList<PotFundingLine> handSplitLines, Guid operationId)
    {
        var bank = income.Account ?? throw new ArgumentException("Choose the bank this income landed in.");
        var amount = IncomeAmount(income.NetValue);
        IReadOnlyList<PotFundingLine> fundingLines = handSplitLines.Count > 0 ? handSplitLines : [new(bank, amount)];

        return Build(
            potsAccount, amount, income.Date, income.Description ?? string.Empty, pot: null, fundingLines, operationId,
            income, $"Deposit to {potsAccount.Name} – {income.IncomeSource.Name}");
    }

    public static IReadOnlyList<PotTransaction> OlderIncomeTransactions(Income income) =>
        Allocate(ActivePots(income.PotsAccount), IncomeAmount(income.NetValue), income.Date, income.Description ?? string.Empty, income, operationId: null);

    private static decimal IncomeAmount(decimal netValue) =>
        MoneyRounding.ToCents(TitheRule.NetOfTithe(netValue));

    private static PotOperation Build(
        Account potsAccount,
        decimal amount,
        DateOnly date,
        string description,
        Pot? pot,
        IReadOnlyList<PotFundingLine> fundingLines,
        Guid operationId,
        Income? income,
        string expenseDescription)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.");
        }

        if (pot is not null)
        {
            potsAccount.EnsureOwnsPot(pot.Id);
        }

        EnsureFundingLinesAddUp(potsAccount, amount, fundingLines);

        var transactions = pot is null
            ? SplitAcrossActivePots(potsAccount, amount, date, description, operationId, income)
            : [PotTransaction.Create(pot, amount, date, description, operationId: operationId)];

        if (fundingLines.Count == 0)
        {
            return new(operationId, potsAccount, transactions, []);
        }

        var potsCategory = potsAccount.Category
            ?? throw new ArgumentException($"Set a category on {potsAccount.Name} first.");

        var expenses = fundingLines
            .Select(line => Expense.Create(
                date, expenseDescription, line.Amount, potsCategory, line.Bank, creditCard: null,
                countsAsTithe: false, operationId: operationId))
            .ToList();

        return new(operationId, potsAccount, transactions, expenses);
    }

    private static void EnsureFundingLinesAddUp(Account potsAccount, decimal amount, IReadOnlyList<PotFundingLine> fundingLines)
    {
        if (fundingLines.Count > MaxFundingLines)
        {
            throw new ArgumentException($"A deposit has at most {MaxFundingLines} funding lines.");
        }

        if (fundingLines.Any(line => line.Amount <= 0))
        {
            throw new ArgumentException("Funding line amounts must be greater than zero.");
        }

        if (fundingLines.Count > 0 && fundingLines.Sum(line => line.Amount) != amount)
        {
            throw new ArgumentException($"Funding lines must add up to {Money(potsAccount.Currency, amount)}.");
        }
    }

    private static List<PotTransaction> SplitAcrossActivePots(
        Account potsAccount, decimal amount, DateOnly date, string description, Guid operationId, Income? income)
    {
        var activePots = ActivePots(potsAccount);
        if (activePots.Count == 0)
        {
            throw new ArgumentException($"{potsAccount.Name} has no active pots.");
        }

        Account.EnsureActiveSplitTotal(activePots.Sum(pot => pot.SplitPercentage));

        return Allocate(activePots, amount, date, description, income, operationId);
    }

    private static List<Pot> ActivePots(Account? potsAccount) =>
        (potsAccount?.Pots ?? []).Where(pot => pot.IsActive).ToList();

    private static List<PotTransaction> Allocate(
        IReadOnlyList<Pot> pots, decimal amount, DateOnly date, string description, Income? income, Guid? operationId)
    {
        var amounts = PotSplitAllocator.Allocate(pots, amount);
        return pots
            .Select((pot, index) => PotTransaction.Create(pot, amounts[index], date, description, income, operationId))
            .ToList();
    }

    private static string Money(Currency currency, decimal amount)
    {
        var symbol = currency switch
        {
            Currency.GBP => "£",
            Currency.BRL => "R$",
            Currency.USD => "$",
            _ => throw new ArgumentOutOfRangeException(nameof(currency), currency, null)
        };

        return FormattableString.Invariant($"{symbol}{amount:F2}");
    }
}
