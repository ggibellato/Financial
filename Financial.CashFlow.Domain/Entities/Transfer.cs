using System;

namespace Financial.CashFlow.Domain.Entities;

public class Transfer
{
    public Guid Id { get; private set; }
    public DateOnly Date { get; private set; }
    public Bank SourceBank { get; private set; } = null!;
    public Bank DestinationBank { get; private set; } = null!;
    public decimal Amount { get; private set; }
    public string? Note { get; private set; }

    private Transfer() { }

    public static Transfer Create(
        DateOnly date,
        Bank sourceBank,
        Bank destinationBank,
        decimal amount,
        string? note)
    {
        Validate(sourceBank, destinationBank, amount);

        return new()
        {
            Id = Guid.NewGuid(),
            Date = date,
            SourceBank = sourceBank,
            DestinationBank = destinationBank,
            Amount = amount,
            Note = note
        };
    }

    public void UpdateDetails(
        DateOnly date,
        Bank sourceBank,
        Bank destinationBank,
        decimal amount,
        string? note)
    {
        Validate(sourceBank, destinationBank, amount);

        Date = date;
        SourceBank = sourceBank;
        DestinationBank = destinationBank;
        Amount = amount;
        Note = note;
    }

    private static void Validate(Bank sourceBank, Bank destinationBank, decimal amount)
    {
        if (sourceBank is null)
        {
            throw new ArgumentException("Source bank is required.");
        }

        if (destinationBank is null)
        {
            throw new ArgumentException("Destination bank is required.");
        }

        if (sourceBank.Id == destinationBank.Id)
        {
            throw new ArgumentException("A transfer must move money between two different banks.");
        }

        if (amount <= 0)
        {
            throw new ArgumentException("Transfer amount must be greater than zero.");
        }
    }
}
