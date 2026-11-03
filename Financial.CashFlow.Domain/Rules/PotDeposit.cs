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
        ReserveBucket? pot,
        IReadOnlyList<PotFundingLine> fundingLines,
        Guid operationId)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Amount must be greater than zero.");
        }

        if (pot is not null && !potsAccount.OwnsPot(pot.Id))
        {
            throw new ArgumentException($"That pot isn't in {potsAccount.Name}.");
        }

        EnsureFundingLinesAddUp(potsAccount, amount, fundingLines);

        var movements = pot is null
            ? SplitAcrossActivePots(potsAccount, amount, date, description, operationId)
            : [ReserveMovement.Create(pot, amount, date, description, operationId: operationId)];

        if (fundingLines.Count == 0)
        {
            return new(operationId, potsAccount, movements, []);
        }

        var potsCategory = potsAccount.Category
            ?? throw new ArgumentException($"Set a category on {potsAccount.Name} first.");

        var expenses = fundingLines
            .Select(line => Expense.Create(
                date, $"Deposit to {potsAccount.Name}", line.Amount, potsCategory, line.Bank, creditCard: null,
                countsAsTithe: false, operationId: operationId))
            .ToList();

        return new(operationId, potsAccount, movements, expenses);
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

    private static List<ReserveMovement> SplitAcrossActivePots(
        Account potsAccount, decimal amount, DateOnly date, string description, Guid operationId)
    {
        var activePots = (potsAccount.Pots ?? []).Where(pot => pot.IsActive).ToList();
        if (activePots.Count == 0)
        {
            throw new ArgumentException($"{potsAccount.Name} has no active pots.");
        }

        var splitTotal = activePots.Sum(pot => pot.SplitPercentage);
        if (splitTotal != 100m)
        {
            throw new ArgumentException(FormattableString.Invariant($"Active pots must total 100% (now {splitTotal:0.##}%)."));
        }

        var amounts = ReserveSplitAllocator.Allocate(activePots, amount);
        return activePots
            .Select((pot, index) => ReserveMovement.Create(pot, amounts[index], date, description, operationId: operationId))
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
