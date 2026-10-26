using System;
using Financial.CashFlow.Domain.Enums;
using Financial.CashFlow.Domain.Rules;
using Financial.Shared.Abstractions.Currencies;

namespace Financial.CashFlow.Domain.Entities.Transactions;

public class Transfer : IAccountTransaction
{
    public const int MaxNoteLength = 200;

    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public Account SourceAccount { get; private set; } = null!;
    public Account DestinationAccount { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string? Note { get; private set; }
    public decimal? DestinationAmount { get; private set; }
    public bool IsStatementPayment => DestinationAccount.Has(Capability.ReceivesPayments);

    private Transfer() { }

    public static Transfer Create(
        DateOnly date,
        Account sourceAccount,
        Account destinationAccount,
        decimal amount,
        decimal? destinationAmount,
        string? note,
        DateOnly today)
    {
        var transfer = new Transfer { Id = Guid.NewGuid() };
        transfer.UpdateDetails(date, sourceAccount, destinationAccount, amount, destinationAmount, note, today);
        return transfer;
    }

    public static Transfer CreateStatementPayment(DateOnly date, Account bank, Account card, decimal amount, DateOnly today)
    {
        EnsurePairIsValid(bank, card, TransferPairRules.StatementPayment);
        ActiveAccountRule.EnsureUsable(null, bank);
        if (bank.Currency != card.Currency)
        {
            throw new ArgumentException($"The paying account must be in the card's currency ({card.Currency}).");
        }

        EnsureAmountsAreValid(amount, null, bank.Currency, card.Currency);
        EnsureDateAndNoteAreValid(date, null, today);

        return new Transfer
        {
            Id = Guid.NewGuid(),
            Date = date,
            SourceAccount = bank,
            DestinationAccount = card,
            Amount = amount
        };
    }

    public void UpdateDetails(
        DateOnly date,
        Account sourceAccount,
        Account destinationAccount,
        decimal amount,
        decimal? destinationAmount,
        string? note,
        DateOnly today)
    {
        EnsurePairIsValid(sourceAccount, destinationAccount, TransferPairRules.Default);
        ActiveAccountRule.EnsureUsable(SourceAccount, sourceAccount);
        ActiveAccountRule.EnsureUsable(DestinationAccount, destinationAccount);
        EnsureAmountsAreValid(amount, destinationAmount, sourceAccount.Currency, destinationAccount.Currency);
        EnsureDateAndNoteAreValid(date, note, today);

        Date = date;
        SourceAccount = sourceAccount;
        DestinationAccount = destinationAccount;
        Amount = amount;
        DestinationAmount = destinationAmount;
        Note = note;
    }

    public decimal AmountReceived() => DestinationAmount ?? Amount;

    public decimal ContributionTo(Account account) =>
        (DestinationAccount.Id == account.Id ? AmountReceived() : 0m) - (SourceAccount.Id == account.Id ? Amount : 0m);

    private static void EnsurePairIsValid(Account sourceAccount, Account destinationAccount, TransferPairRules pairRules)
    {
        if (sourceAccount is null)
        {
            throw new ArgumentException("Source account is required.");
        }

        if (destinationAccount is null)
        {
            throw new ArgumentException("Destination account is required.");
        }

        if (sourceAccount.Id == destinationAccount.Id)
        {
            throw new ArgumentException("Source and destination must be different accounts.");
        }

        if (!pairRules.Allows(sourceAccount, destinationAccount))
        {
            throw new ArgumentException($"Transfers from {sourceAccount.Name} to {destinationAccount.Name} are not allowed.");
        }
    }

    private static void EnsureAmountsAreValid(decimal amount, decimal? destinationAmount, Currency sourceCurrency, Currency destinationCurrency)
    {
        if (amount <= 0)
        {
            throw new ArgumentException("Transfer amount must be greater than zero.");
        }

        var crossCurrency = sourceCurrency != destinationCurrency;

        if (!crossCurrency && destinationAmount is not null)
        {
            throw new ArgumentException("Destination amount only applies when the currencies differ.");
        }

        if (crossCurrency && destinationAmount is null)
        {
            throw new ArgumentException($"Enter the amount received in {destinationCurrency}.");
        }

        if (destinationAmount <= 0)
        {
            throw new ArgumentException("Destination amount must be greater than zero.");
        }
    }

    private static void EnsureDateAndNoteAreValid(DateOnly date, string? note, DateOnly today)
    {
        if (date > today)
        {
            throw new ArgumentException("Date must not be in the future.");
        }

        if (note?.Length > MaxNoteLength)
        {
            throw new ArgumentException($"Note must be {MaxNoteLength} characters or fewer.");
        }
    }
}
