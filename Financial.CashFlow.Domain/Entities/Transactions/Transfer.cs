using System;
using Financial.CashFlow.Domain.Enums;

namespace Financial.CashFlow.Domain.Entities.Transactions;

public class Transfer : IAccountTransaction
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public Account SourceAccount { get; private set; } = null!;
    public Account DestinationAccount { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string? Note { get; private set; }

    private Transfer() { }

    public static Transfer Create(
        DateOnly date,
        Account sourceAccount,
        Account destinationAccount,
        decimal amount,
        string? note)
    {
        Validate(sourceAccount, destinationAccount, amount);

        return new()
        {
            Id = Guid.NewGuid(),
            Date = date,
            SourceAccount = sourceAccount,
            DestinationAccount = destinationAccount,
            Amount = amount,
            Note = note
        };
    }

    public void UpdateDetails(
        DateOnly date,
        Account sourceAccount,
        Account destinationAccount,
        decimal amount,
        string? note)
    {
        Validate(sourceAccount, destinationAccount, amount);

        Date = date;
        SourceAccount = sourceAccount;
        DestinationAccount = destinationAccount;
        Amount = amount;
        Note = note;
    }

    public decimal ContributionTo(Account account) =>
        (DestinationAccount.Id == account.Id ? Amount : 0m) - (SourceAccount.Id == account.Id ? Amount : 0m);

    private static void Validate(Account sourceAccount, Account destinationAccount, decimal amount)
    {
        if (sourceAccount is null)
        {
            throw new ArgumentException("Source bank is required.");
        }

        if (destinationAccount is null)
        {
            throw new ArgumentException("Destination bank is required.");
        }

        if (sourceAccount.Id == destinationAccount.Id)
        {
            throw new ArgumentException("A transfer must move money between two different banks.");
        }

        if (!sourceAccount.Has(Capability.CanSendTransfers))
        {
            throw new ArgumentException($"Account '{sourceAccount.Name}' cannot send transfers.");
        }

        if (!destinationAccount.Has(Capability.CanReceiveTransfers))
        {
            throw new ArgumentException($"Account '{destinationAccount.Name}' cannot receive transfers.");
        }

        if (amount <= 0)
        {
            throw new ArgumentException("Transfer amount must be greater than zero.");
        }
    }
}
